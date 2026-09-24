using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.NewsService;

public sealed class Service : IService
{
    private const int MinimumPublishedContentLength = 300;
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.CreateNewsResponse> CreateNewsAsync(
        Request.CreateNewsRequest request,
        Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Parse trạng thái tạo bài viết.
        var isStatusParsed = Enum.TryParse<NewsStatus>(
            request.Status,
            ignoreCase: false,
            out var status);

        if (!isStatusParsed || !Enum.IsDefined(status) ||
            (status != NewsStatus.Draft && status != NewsStatus.Published))
        {
            throw new NewsException(
                "NEWS_STATUS_INVALID",
                "Trạng thái tạo bài viết không hợp lệ.",
                "status");
        }

        // 2. Validate dữ liệu bắt buộc theo trạng thái đích.
        var title = request.Title?.Trim();
        var summary = request.Summary?.Trim();
        var content = request.Content?.Trim();
        var requiredFields = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            requiredFields.Add("title");
        }

        if (status == NewsStatus.Published)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                requiredFields.Add("summary");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                requiredFields.Add("content");
            }
        }

        if (requiredFields.Count > 0)
        {
            throw new NewsException(
                "NEWS_VALIDATION_ERROR",
                "Thông tin bài viết không hợp lệ.",
                requiredFields.ToArray());
        }

        if (status == NewsStatus.Published &&
            !HasMinimumPublishedContentLength(content!))
        {
            throw new NewsException(
                "NEWS_CONTENT_TOO_SHORT",
                "Nội dung bài viết phải có ít nhất 300 ký tự.",
                "content");
        }

        var categoryIds = request.CategoryIds ?? new List<Guid>();

        if (status == NewsStatus.Published && categoryIds.Count == 0)
        {
            throw new NewsException(
                "NEWS_CATEGORY_REQUIRED",
                "Vui lòng chọn ít nhất một danh mục để đăng bài viết.",
                "categoryIds");
        }

        var hasDuplicateCategory = categoryIds
            .GroupBy(categoryId => categoryId)
            .Any(group => group.Count() > 1);

        if (hasDuplicateCategory)
        {
            throw new NewsException(
                "NEWS_CATEGORY_INVALID",
                "Danh mục bài viết không được trùng lặp.",
                "categoryIds");
        }

        // 3. Kiểm tra tác giả và danh mục trước khi mở transaction ghi.
        var creator = await _dbContext.Users
            .AsNoTracking()
            .SingleAsync(user => user.Id == createdBy);

        var categories = await _dbContext.NewsCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .ToListAsync();

        if (categories.Count != categoryIds.Count)
        {
            throw new NewsException(
                "NEWS_CATEGORY_INVALID",
                "Một hoặc nhiều danh mục không tồn tại.",
                "categoryIds");
        }

        // 4. Lưu bài viết và category links trong cùng một transaction.
        var nowUtc = DateTimeOffset.UtcNow;
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = title!,
            Summary = summary,
            Content = content,
            Status = status,
            Published = status == NewsStatus.Published,
            CreatedBy = createdBy,
            CreatedAt = nowUtc,
            UpdatedAt = null,
            PublishAt = status == NewsStatus.Published ? nowUtc : null
        };

        var categoryLinks = categories
            .Select(category => new NewsArticleCategory
            {
                Id = Guid.NewGuid(),
                NewsArticleId = article.Id,
                NewsCategoryId = category.Id
            })
            .ToList();

        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.NewsArticles.Add(article);
            _dbContext.NewsArticleCategories.AddRange(categoryLinks);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new NewsException(
                "NEWS_ARTICLE_CREATE_FAILED",
                "Không thể tạo bài viết.",
                exception);
        }

        // 5. Trả dữ liệu đã lưu theo contract create.
        return new Response.CreateNewsResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            Content = article.Content,
            AuthorName = creator.FullName,
            CreatedAt = article.CreatedAt,
            UpdatedAt = null,
            PublishAt = article.PublishAt,
            Status = GetDisplayName(article.Status),
            Categories = categories
                .Select(category => new Response.NewsCategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name
                })
            .ToList()
        };
    }

    public async Task<Response.UpdateNewsResponse> UpdateNewsAsync(
        Guid id,
        Request.UpdateNewsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Đọc bài viết hiện tại để kiểm tra trạng thái và thay category links.
        var article = await _dbContext.NewsArticles
            .Include(item => item.Creator)
            .Include(item => item.NewsArticleCategories)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (article is null)
        {
            throw new NewsException(
                "NEWS_ARTICLE_NOT_FOUND",
                "Không tìm thấy bài viết.",
                "id");
        }

        if (article.Status == NewsStatus.Closed)
        {
            throw new NewsException(
                "NEWS_ARTICLE_CLOSED",
                "Bài viết đã đóng không thể chỉnh sửa hoặc đăng lại.",
                "id");
        }

        // 2. Parse trạng thái đích và kiểm tra transition từ trạng thái hiện tại.
        var isStatusParsed = Enum.TryParse<NewsStatus>(
            request.Status,
            ignoreCase: false,
            out var targetStatus);

        if (!isStatusParsed || !Enum.IsDefined(targetStatus))
        {
            throw new NewsException(
                "NEWS_VALIDATION_ERROR",
                "Thông tin cập nhật bài viết không hợp lệ.",
                "status");
        }

        var isValidTransition = false;

        if (article.Status == NewsStatus.Draft)
        {
            isValidTransition = targetStatus == NewsStatus.Draft ||
                targetStatus == NewsStatus.Published;
        }
        else if (article.Status == NewsStatus.Published)
        {
            isValidTransition = targetStatus == NewsStatus.Published ||
                targetStatus == NewsStatus.Closed;
        }

        if (!isValidTransition)
        {
            throw new NewsException(
                "NEWS_STATUS_TRANSITION_INVALID",
                "Không thể chuyển bài viết sang trạng thái đã chọn.",
                "status");
        }

        // 3. Validate nội dung theo trạng thái đích.
        var title = request.Title?.Trim();
        var summary = request.Summary?.Trim();
        var content = request.Content?.Trim();
        var requiredFields = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            requiredFields.Add("title");
        }

        if (targetStatus == NewsStatus.Published)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                requiredFields.Add("summary");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                requiredFields.Add("content");
            }
        }

        if (requiredFields.Count > 0)
        {
            throw new NewsException(
                "NEWS_VALIDATION_ERROR",
                "Thông tin cập nhật bài viết không hợp lệ.",
                requiredFields.ToArray());
        }

        if (targetStatus == NewsStatus.Published &&
            !HasMinimumPublishedContentLength(content!))
        {
            throw new NewsException(
                "NEWS_CONTENT_TOO_SHORT",
                "Nội dung bài viết phải có ít nhất 300 ký tự.",
                "content");
        }

        var categoryIds = request.CategoryIds ?? new List<Guid>();

        if (targetStatus == NewsStatus.Published && categoryIds.Count == 0)
        {
            throw new NewsException(
                "NEWS_CATEGORY_REQUIRED",
                "Vui lòng chọn ít nhất một danh mục để đăng bài viết.",
                "categoryIds");
        }

        var hasDuplicateCategory = categoryIds
            .GroupBy(categoryId => categoryId)
            .Any(group => group.Count() > 1);

        if (hasDuplicateCategory)
        {
            throw new NewsException(
                "NEWS_CATEGORY_INVALID",
                "Danh mục bài viết không được trùng lặp.",
                "categoryIds");
        }

        var categories = await _dbContext.NewsCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .ToListAsync();

        if (categories.Count != categoryIds.Count)
        {
            throw new NewsException(
                "NEWS_CATEGORY_INVALID",
                "Một hoặc nhiều danh mục không tồn tại.",
                "categoryIds");
        }

        // 4. Áp dụng trạng thái và thời gian xuất bản theo transition hợp lệ.
        var nowUtc = DateTimeOffset.UtcNow;
        var publishAt = article.PublishAt;

        if (article.Status == NewsStatus.Draft && targetStatus == NewsStatus.Draft)
        {
            publishAt = null;
        }
        else if (article.Status == NewsStatus.Draft && targetStatus == NewsStatus.Published)
        {
            publishAt = nowUtc;
        }

        article.Title = title!;
        article.Summary = summary;
        article.Content = content;
        article.Status = targetStatus;
        article.Published = targetStatus == NewsStatus.Published;
        article.PublishAt = publishAt;
        article.UpdatedAt = nowUtc;

        var newCategoryLinks = categories
            .Select(category => new NewsArticleCategory
            {
                Id = Guid.NewGuid(),
                NewsArticleId = article.Id,
                NewsCategoryId = category.Id
            })
            .ToList();

        // 5. Lưu nội dung, trạng thái và category links trong cùng một transaction.
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.NewsArticleCategories.RemoveRange(article.NewsArticleCategories);
            _dbContext.NewsArticleCategories.AddRange(newCategoryLinks);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new NewsException(
                "NEWS_ARTICLE_UPDATE_FAILED",
                "Không thể cập nhật bài viết.",
                exception);
        }

        // 6. Trả dữ liệu bài viết sau cập nhật theo contract TDD-010.
        return new Response.UpdateNewsResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            Content = article.Content,
            AuthorName = article.Creator.FullName,
            CreatedAt = article.CreatedAt,
            UpdatedAt = ConvertUpdatedAtToVietnamDate(article.UpdatedAt),
            PublishAt = article.PublishAt,
            Status = GetDisplayName(article.Status),
            Categories = categories
                .Select(category => new Response.NewsCategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name
                })
                .ToList()
        };
    }

    private static bool HasMinimumPublishedContentLength(string content)
    {
        var nonWhitespaceCharacterCount = 0;

        foreach (var character in content)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            nonWhitespaceCharacterCount++;

            if (nonWhitespaceCharacterCount >= MinimumPublishedContentLength)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<Response.PagedNewsListResponse> GetNewsListAsync(
        Request.GetNewsListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
        {
            var fields = new List<string>();

            if (request.Page < 1)
            {
                fields.Add("page");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                fields.Add("pageSize");
            }

            throw new NewsException(
                "NEWS_QUERY_INVALID",
                "Thông tin phân trang không hợp lệ.",
                fields.ToArray());
        }

        var search = request.Search?.Trim();
        if (search is { Length: > 300 })
        {
            throw new NewsException(
                "NEWS_QUERY_INVALID",
                "Từ khóa tìm kiếm không được vượt quá 300 ký tự.",
                "search");
        }

        NewsStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();

            var statusParsed = Enum.TryParse<NewsStatus>(
                status,
                ignoreCase: false,
                out var parsedStatus);

            if (!statusParsed)
            {
                throw new NewsException(
                    "NEWS_QUERY_INVALID",
                    "Trạng thái lọc không hợp lệ.",
                    "status");
            }

            var statusDefined = Enum.IsDefined(parsedStatus);
            if (!statusDefined)
            {
                throw new NewsException(
                    "NEWS_QUERY_INVALID",
                    "Trạng thái lọc không hợp lệ.",
                    "status");
            }

            statusFilter = parsedStatus;
        }

        var query = _dbContext.NewsArticles
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(article =>
                article.Title.ToLower().Contains(searchLower));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(article =>
                article.Status == statusFilter.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(article => article.NewsArticleCategories
                .Any(link => link.NewsCategoryId == request.CategoryId.Value));
        }

        var totalItems = await query.CountAsync();

        var articleRows = await query
            .OrderByDescending(article => article.CreatedAt)
            .ThenBy(article => article.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(article => new
            {
                article.Id,
                article.Title,
                AuthorName = article.Creator!.FullName,
                article.CreatedAt,
                article.PublishAt,
                article.Status
            })
            .ToListAsync();

        var articleIds = articleRows
            .Select(article => article.Id)
            .ToList();

        var categoryRows = await _dbContext.NewsArticleCategories
            .AsNoTracking()
            .Where(link => articleIds.Contains(link.NewsArticleId))
            .Select(link => new
            {
                ArticleId = link.NewsArticleId,
                CategoryId = link.NewsCategoryId,
                CategoryName = link.NewsCategory.Name
            })
            .ToListAsync();

        var categoriesByArticleId = categoryRows
            .GroupBy(category => category.ArticleId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(category => category.CategoryName)
                    .ThenBy(category => category.CategoryId)
                    .Select(category => new Response.NewsCategoryResponse
                    {
                        Id = category.CategoryId,
                        Name = category.CategoryName
                    })
                    .ToList());

        var items = articleRows
            .Select(article => new Response.NewsListItemResponse
            {
                Id = article.Id,
                Title = article.Title,
                AuthorName = article.AuthorName,
                CreatedAt = article.CreatedAt,
                PublishAt = article.PublishAt,
                Status = GetDisplayName(article.Status),
                Categories = GetCategories(categoriesByArticleId, article.Id)
            })
            .ToList();

        return new Response.PagedNewsListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)request.PageSize)
        };
    }

    public async Task<Response.NewsDetailResponse> GetNewsDetailAsync(Guid id)
    {
        // 1. Đọc bài viết cùng tác giả và toàn bộ danh mục đang được gắn.
        var article = await _dbContext.NewsArticles
            .AsNoTracking()
            .Include(item => item.Creator)
            .Include(item => item.NewsArticleCategories)
            .ThenInclude(item => item.NewsCategory)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (article is null)
        {
            throw new NewsException(
                "NEWS_ARTICLE_NOT_FOUND",
                "Không tìm thấy bài viết.",
                "id");
        }

        // 2. Suy ra các thao tác được phép từ trạng thái hiện tại của bài viết.
        var actions = new List<string>();

        if (article.Status == NewsStatus.Draft)
        {
            actions.Add("Edit");
            actions.Add("Publish");
        }
        else if (article.Status == NewsStatus.Published)
        {
            actions.Add("Edit");
            actions.Add("Close");
        }
        else if (article.Status != NewsStatus.Closed)
        {
            throw new ArgumentOutOfRangeException(
                nameof(article.Status),
                article.Status,
                "Trạng thái bài viết không hợp lệ.");
        }

        // 3. Map dữ liệu đọc được sang contract detail; không thay đổi database.
        return new Response.NewsDetailResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            Content = article.Content,
            AuthorName = article.Creator.FullName,
            CreatedAt = article.CreatedAt,
            UpdatedAt = ConvertUpdatedAtToVietnamDate(article.UpdatedAt),
            PublishAt = article.PublishAt,
            Status = GetDisplayName(article.Status),
            Categories = article.NewsArticleCategories
                .OrderBy(link => link.NewsCategory.Name)
                .ThenBy(link => link.NewsCategory.Id)
                .Select(link => new Response.NewsCategoryResponse
                {
                    Id = link.NewsCategory.Id,
                    Name = link.NewsCategory.Name
                })
                .ToList(),
            Actions = actions
        };
    }

    public async Task<List<Response.NewsCategoryResponse>> GetNewsCategoriesAsync()
    {
        return await _dbContext.NewsCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new Response.NewsCategoryResponse
            {
                Id = category.Id,
                Name = category.Name
            })
            .ToListAsync();
    }

    private static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).Single();

        return member.GetCustomAttributes(typeof(DisplayAttribute), inherit: false)
            .OfType<DisplayAttribute>()
            .SingleOrDefault()?
            .GetName() ?? value.ToString();
    }

    private static List<Response.NewsCategoryResponse> GetCategories(
        Dictionary<Guid, List<Response.NewsCategoryResponse>> categoriesByArticleId,
        Guid articleId)
    {
        if (categoriesByArticleId.ContainsKey(articleId))
        {
            return categoriesByArticleId[articleId];
        }

        return new List<Response.NewsCategoryResponse>();
    }

    private static DateOnly? ConvertUpdatedAtToVietnamDate(DateTimeOffset? updatedAt)
    {
        if (!updatedAt.HasValue)
        {
            return null;
        }

        var updatedAtInVietnam = updatedAt.Value.ToOffset(VietnamUtcOffset);
        return DateOnly.FromDateTime(updatedAtInVietnam.DateTime);
    }
}
