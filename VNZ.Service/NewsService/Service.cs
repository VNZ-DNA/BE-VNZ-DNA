using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.NewsService;

public sealed class Service : IService
{
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
