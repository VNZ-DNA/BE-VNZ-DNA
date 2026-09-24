using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.NewsService;

public sealed class Service : IService
{
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
                AuthorName = article.Creator == null
                    ? null
                    : article.Creator.FullName,
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
}
