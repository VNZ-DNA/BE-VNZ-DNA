using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using MediaService = VNZ.Service.Utils.MediaService;
using RichTextService = VNZ.Service.Utils.RichTextService;

namespace VNZ.Service.NewsService;

public sealed class Service : IService
{
    private const int MinimumPublishedContentLength = 300;
    private const int WordsPerMinute = 200;
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly MediaService.IService _mediaService;
    private readonly RichTextService.IService _richTextService;

    public Service(
        AppDbContext dbContext,
        MediaService.IService mediaService,
        RichTextService.IService richTextService)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
        _richTextService = richTextService;
    }

    public async Task<Response.DeleteNewsResponse> DeleteNewsAsync(Guid id)
    {
        var affectedRows = await _dbContext.NewsArticles
            .Where(article => article.Id == id &&
                              !article.IsDelete &&
                              !(article.Status == NewsStatus.Published &&
                                article.Published &&
                                article.PublishAt.HasValue))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(article => article.IsDelete, true));

        if (affectedRows == 1)
        {
            return new Response.DeleteNewsResponse { Id = id };
        }

        var articleExists = await _dbContext.NewsArticles
            .AsNoTracking()
            .AnyAsync(article => article.Id == id && !article.IsDelete);

        if (!articleExists)
        {
            throw new NewsException("NEWS_ARTICLE_NOT_FOUND", "Không tìm thấy bài viết.", "id");
        }

        throw new NewsException(
            "NEWS_DELETE_FORBIDDEN",
            "Không thể xóa bài viết đang hiển thị trên website.");
    }

    public async Task<Response.UploadContentImageResponse> UploadContentImageAsync(
        Request.UploadContentImageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var uploadResult = await _mediaService.UploadImageAsync(
            new MediaService.Request.UploadImageRequest
            {
                File = request.File,
                Purpose = "NewsImage"
            });

        return new Response.UploadContentImageResponse
        {
            Url = uploadResult.Url,
            PublicId = uploadResult.PublicId,
            Format = uploadResult.Format,
            Bytes = uploadResult.Bytes,
            Width = uploadResult.Width,
            Height = uploadResult.Height
        };
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
        var summary = _richTextService.SanitizeNewsSummary(request.Summary?.Trim());
        var content = SanitizeNewsContent(request.Content?.Trim());
        var requiredFields = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            requiredFields.Add("title");
        }
        else if (ContainsHtmlTag(title))
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

        string? imageUrl = null;

        if (request.Image is not null)
        {
            var uploadResult = await _mediaService.UploadImageAsync(
                new MediaService.Request.UploadImageRequest
                {
                    File = request.Image,
                    Purpose = "NewsImage"
                });

            imageUrl = uploadResult.Url;
        }

        // 4. Lưu bài viết và category links trong cùng một transaction.
        var nowUtc = DateTimeOffset.UtcNow;
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = title!,
            Summary = summary,
            Content = content,
            ImageUrl = imageUrl,
            ReadingTimeMinutes = CalculateReadingTimeMinutes(content),
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
            ImageUrl = article.ImageUrl,
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
        var imageAction = string.IsNullOrWhiteSpace(request.Action)
            ? null
            : request.Action.Trim();

        if (!string.IsNullOrWhiteSpace(imageAction) &&
            !string.Equals(imageAction, "removeImage", StringComparison.Ordinal))
        {
            throw new NewsException(
                "NEWS_IMAGE_ACTION_INVALID",
                "Thao tác ảnh đại diện không hợp lệ.",
                "action");
        }

        if (string.Equals(imageAction, "removeImage", StringComparison.Ordinal) &&
            request.Image is not null)
        {
            throw new NewsException(
                "NEWS_IMAGE_ACTION_INVALID",
                "Không thể vừa gỡ ảnh đại diện vừa gửi ảnh mới.",
                "action",
                "image");
        }

        var title = request.Title?.Trim();
        var summary = _richTextService.SanitizeNewsSummary(request.Summary?.Trim());
        var content = SanitizeNewsContent(request.Content?.Trim());
        var requiredFields = new List<string>();

        if (string.IsNullOrWhiteSpace(title))
        {
            requiredFields.Add("title");
        }
        else if (ContainsHtmlTag(title))
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

        var imageUrl = string.Equals(imageAction, "removeImage", StringComparison.Ordinal)
            ? null
            : article.ImageUrl;

        if (imageAction is null && request.Image is not null)
        {
            var uploadResult = await _mediaService.UploadImageAsync(
                new MediaService.Request.UploadImageRequest
                {
                    File = request.Image,
                    Purpose = "NewsImage"
                });

            imageUrl = uploadResult.Url;
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
        article.ImageUrl = imageUrl;
        article.ReadingTimeMinutes = CalculateReadingTimeMinutes(content);
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
            ImageUrl = article.ImageUrl,
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

    private bool HasMinimumPublishedContentLength(string content)
    {
        var plainText = _richTextService.ToPlainText(content);
        var nonWhitespaceCharacterCount = 0;

        foreach (var character in plainText)
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

    private int CalculateReadingTimeMinutes(string? content)
    {
        var plainText = _richTextService.ToPlainText(content ?? string.Empty);
        var wordCount = plainText
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Length;

        return Math.Max(1, (int)Math.Ceiling(wordCount / (double)WordsPerMinute));
    }

    private string? SanitizeNewsContent(string? value)
    {
        try
        {
            return _richTextService.SanitizeNewsContent(value);
        }
        catch (RichTextService.RichTextValidationException)
        {
            throw new NewsException(
                "NEWS_CONTENT_INVALID",
                "News content is invalid.",
                "content");
        }
    }

    private string? SafeNormalizeNewsContent(string? value)
    {
        try
        {
            return _richTextService.SanitizeNewsContent(value);
        }
        catch (RichTextService.RichTextValidationException)
        {
            return _richTextService.Sanitize(value, allowLinks: true);
        }
    }

    private static bool ContainsHtmlTag(string value)
    {
        return Regex.IsMatch(
            value,
            "<[^>]*>",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);
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

        var statusFilters = new HashSet<NewsStatus>();
        if (request.Status is not null)
        {
            foreach (var statusValue in request.Status)
            {
                var status = statusValue?.Trim();
                var parsedStatus = default(NewsStatus);
                var statusParsed = !string.IsNullOrWhiteSpace(status) &&
                    Enum.TryParse<NewsStatus>(status, ignoreCase: false, out parsedStatus);

                if (!statusParsed || !Enum.IsDefined(parsedStatus))
                {
                    throw new NewsException(
                        "NEWS_QUERY_INVALID",
                        "Trạng thái lọc không hợp lệ.",
                        "status");
                }

                statusFilters.Add(parsedStatus);
            }
        }

        var categoryIds = new HashSet<Guid>();
        if (request.CategoryId is not null)
        {
            foreach (var categoryIdValue in request.CategoryId)
            {
                var isCategoryIdValid = Guid.TryParse(categoryIdValue, out var categoryId) &&
                    categoryId != Guid.Empty;

                if (!isCategoryIdValid)
                {
                    throw new NewsException(
                        "NEWS_QUERY_INVALID",
                        "Danh mục lọc không hợp lệ.",
                        "categoryId");
                }

                categoryIds.Add(categoryId);
            }
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

        if (statusFilters.Count > 0)
        {
            query = query.Where(article =>
                statusFilters.Contains(article.Status));
        }

        if (categoryIds.Count > 0)
        {
            query = query.Where(article => article.NewsArticleCategories
                .Any(link => categoryIds.Contains(link.NewsCategoryId)));
        }

        var createdAtFilter = ParseDateFilter(request.CreatedAt, "createdAt");
        var publishAtFilter = ParseDateFilter(request.PublishAt, "publishAt");

        if (createdAtFilter.ExactDate.HasValue)
        {
            var createdAtRange = GetVietnamDateRange(createdAtFilter.ExactDate.Value);
            query = query.Where(article =>
                article.CreatedAt >= createdAtRange.StartUtc &&
                article.CreatedAt < createdAtRange.EndUtc);
        }

        if (publishAtFilter.ExactDate.HasValue)
        {
            var publishAtRange = GetVietnamDateRange(publishAtFilter.ExactDate.Value);
            query = query.Where(article =>
                article.PublishAt.HasValue &&
                article.PublishAt.Value >= publishAtRange.StartUtc &&
                article.PublishAt.Value < publishAtRange.EndUtc);
        }

        var totalItems = await query.CountAsync();
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        if (request.Page > totalPages)
        {
            return new Response.PagedNewsListResponse
            {
                Items = new List<Response.NewsListItemResponse>(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        IOrderedQueryable<NewsArticle>? orderedQuery = null;

        if (createdAtFilter.SortDirection == DateFilterSortDirection.Ascending)
        {
            orderedQuery = query.OrderBy(article => article.CreatedAt);
        }
        else if (createdAtFilter.SortDirection == DateFilterSortDirection.Descending)
        {
            orderedQuery = query.OrderByDescending(article => article.CreatedAt);
        }

        if (publishAtFilter.SortDirection == DateFilterSortDirection.Ascending)
        {
            orderedQuery = orderedQuery is null
                ? query
                    .OrderBy(article => !article.PublishAt.HasValue)
                    .ThenBy(article => article.PublishAt)
                : orderedQuery
                    .ThenBy(article => !article.PublishAt.HasValue)
                    .ThenBy(article => article.PublishAt);
        }
        else if (publishAtFilter.SortDirection == DateFilterSortDirection.Descending)
        {
            orderedQuery = orderedQuery is null
                ? query
                    .OrderByDescending(article => article.PublishAt.HasValue)
                    .ThenByDescending(article => article.PublishAt)
                : orderedQuery
                    .ThenByDescending(article => article.PublishAt.HasValue)
                    .ThenByDescending(article => article.PublishAt);
        }

        orderedQuery ??= query.OrderByDescending(article => article.CreatedAt);

        var articleRows = await orderedQuery
            .ThenBy(article => article.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(article => new
            {
                article.Id,
                article.Title,
                article.ImageUrl,
                AuthorName = article.Creator!.FullName,
                article.CreatedAt,
                article.PublishAt,
                article.Status,
                article.Published
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
                ImageUrl = article.ImageUrl,
                AuthorName = article.AuthorName,
                CreatedAt = article.CreatedAt,
                PublishAt = article.PublishAt,
                Status = GetDisplayName(article.Status),
                CanDelete = !(article.Status == NewsStatus.Published &&
                              article.Published &&
                              article.PublishAt.HasValue),
                DeleteBlockedReason = article.Status == NewsStatus.Published &&
                                      article.Published &&
                                      article.PublishAt.HasValue
                    ? "PUBLIC_VISIBLE"
                    : null,
                Categories = GetCategories(categoriesByArticleId, article.Id)
            })
            .ToList();

        return new Response.PagedNewsListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<Response.PagedPublicNewsListResponse> GetPublicNewsListAsync(
        Request.GetPublicNewsListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate phân trang public theo convention chung của các API list.
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
                "NEWS_PUBLIC_LIST_VALIDATION_FAILED",
                "Tham số phân trang không hợp lệ.",
                fields.ToArray());
        }

        // 2. Chỉ lấy bài có trạng thái và dữ liệu đủ điều kiện public.
        var query = _dbContext.NewsArticles
            .AsNoTracking()
            .Where(article =>
                article.Status == NewsStatus.Published &&
                article.Published &&
                article.PublishAt != null);

        var totalItems = await query.CountAsync();

        // 3. Lấy trang hiện tại với thứ tự publish mới nhất trước.
        var articleRows = await query
            .OrderByDescending(article => article.PublishAt)
            .ThenByDescending(article => article.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(article => new
            {
                article.Id,
                article.Title,
                article.Summary,
                article.PublishAt,
                article.ReadingTimeMinutes
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

        // 4. Summary legacy được sanitize khi đọc để FE chỉ nhận HTML an toàn.
        var items = articleRows
            .Select(article => new Response.PublicNewsListItemResponse
            {
                Id = article.Id,
                Title = article.Title,
                Summary = _richTextService.SanitizeNewsSummary(article.Summary),
                PublishAt = article.PublishAt!.Value,
                ReadingTimeMinutes = article.ReadingTimeMinutes,
                Categories = GetCategories(categoriesByArticleId, article.Id)
            })
            .ToList();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        return new Response.PagedPublicNewsListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<Response.PublicNewsDetailResponse> GetPublicNewsDetailAsync(string id)
    {
        // 1. Validate UUID tại Service để Controller chỉ chịu trách nhiệm nhận route.
        if (!Guid.TryParse(id, out var newsArticleId))
        {
            throw new NewsException(
                "NEWS_PUBLIC_ARTICLE_ID_INVALID",
                "Mã bài viết không hợp lệ.",
                "id");
        }

        // 2. Chỉ đọc bài có đầy đủ điều kiện hiển thị public cùng toàn bộ danh mục.
        var article = await _dbContext.NewsArticles
            .AsNoTracking()
            .Include(item => item.NewsArticleCategories)
            .ThenInclude(item => item.NewsCategory)
            .SingleOrDefaultAsync(item =>
                item.Id == newsArticleId &&
                item.Status == NewsStatus.Published &&
                item.Published &&
                item.PublishAt != null);

        if (article is null)
        {
            throw new NewsException(
                "NEWS_PUBLIC_ARTICLE_NOT_FOUND",
                "Không tìm thấy bài viết công khai.");
        }

        // 3. Nội dung legacy được sanitize khi đọc để FE chỉ nhận HTML an toàn.
        return new Response.PublicNewsDetailResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = _richTextService.SanitizeNewsSummary(article.Summary),
            Content = SafeNormalizeNewsContent(article.Content),
            ImageUrl = article.ImageUrl,
            PublishAt = article.PublishAt!.Value,
            ReadingTimeMinutes = article.ReadingTimeMinutes,
            Categories = article.NewsArticleCategories
                .OrderBy(link => link.NewsCategory.Name)
                .ThenBy(link => link.NewsCategory.Id)
                .Select(link => new Response.NewsCategoryResponse
                {
                    Id = link.NewsCategory.Id,
                    Name = link.NewsCategory.Name
                })
                .ToList()
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
            Summary = _richTextService.SanitizeNewsSummary(article.Summary),
            Content = SafeNormalizeNewsContent(article.Content),
            ImageUrl = article.ImageUrl,
            AuthorName = article.Creator.FullName,
            CreatedAt = article.CreatedAt,
            UpdatedAt = ConvertUpdatedAtToVietnamDate(article.UpdatedAt),
            PublishAt = article.PublishAt,
            Status = GetDisplayName(article.Status),
            CanDelete = !(article.Status == NewsStatus.Published &&
                          article.Published &&
                          article.PublishAt.HasValue),
            DeleteBlockedReason = article.Status == NewsStatus.Published &&
                                  article.Published &&
                                  article.PublishAt.HasValue
                ? "PUBLIC_VISIBLE"
                : null,
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

    private static DateFilter ParseDateFilter(string? value, string field)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return new DateFilter(null, DateFilterSortDirection.None);
        }

        if (string.Equals(normalizedValue, "asc", StringComparison.OrdinalIgnoreCase))
        {
            return new DateFilter(null, DateFilterSortDirection.Ascending);
        }

        if (string.Equals(normalizedValue, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return new DateFilter(null, DateFilterSortDirection.Descending);
        }

        var isDateParsed = DateOnly.TryParseExact(
            normalizedValue,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var exactDate);

        if (isDateParsed && exactDate != DateOnly.MaxValue)
        {
            return new DateFilter(exactDate, DateFilterSortDirection.None);
        }

        throw new NewsException(
            "NEWS_QUERY_INVALID",
            "Bộ lọc ngày không hợp lệ. Dùng YYYY-MM-DD, asc hoặc desc.",
            field);
    }

    private static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetVietnamDateRange(
        DateOnly date)
    {
        var startOfDate = new DateTimeOffset(
            date.ToDateTime(TimeOnly.MinValue),
            VietnamUtcOffset);
        var startOfNextDate = new DateTimeOffset(
            date.AddDays(1).ToDateTime(TimeOnly.MinValue),
            VietnamUtcOffset);

        return (startOfDate.ToUniversalTime(), startOfNextDate.ToUniversalTime());
    }

    private enum DateFilterSortDirection
    {
        None,
        Ascending,
        Descending
    }

    private sealed class DateFilter
    {
        public DateFilter(DateOnly? exactDate, DateFilterSortDirection sortDirection)
        {
            ExactDate = exactDate;
            SortDirection = sortDirection;
        }

        public DateOnly? ExactDate { get; }

        public DateFilterSortDirection SortDirection { get; }
    }
}
