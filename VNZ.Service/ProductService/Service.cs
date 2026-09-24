using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.ProductService;

public sealed class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var search = request.Search?.Trim();

        try
        {
            var query = _dbContext.Products
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.ToLower();
                query = query.Where(product =>
                    product.Name.ToLower().Contains(normalizedSearch));
            }

            var total = await query.CountAsync();

            var products = await query
                .OrderByDescending(product => product.IsPublished)
                .ThenBy(product => product.DisplayOrder)
                .ThenByDescending(product => product.CreatedAt)
                .ThenByDescending(product => product.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = products
                .Select(ToListItemResponse)
                .ToList();

            return new Response.PagedProductListResponse
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
            };
        }
        catch (Exception exception) when (exception is not ProductException &&
                                          exception is not OperationCanceledException)
        {
            throw new ProductException("PRODUCT_LIST_READ_FAILED", "Không thể đọc danh sách sản phẩm.", exception);
        }
    }

    public async Task<List<Response.ProductListItemResponse>> ReorderProductsAsync(
        Request.ReorderProductsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderedProductIds = request.OrderedProductIds;
        if (orderedProductIds is null || orderedProductIds.Count == 0 ||
            orderedProductIds.Distinct().Count() != orderedProductIds.Count)
        {
            throw new ProductException(
                "PRODUCT_ORDER_INVALID",
                "Danh sách sắp xếp Product không hợp lệ.",
                "orderedProductIds");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var publishedProducts = await _dbContext.Products
                .Where(product => product.IsPublished)
                .OrderBy(product => product.DisplayOrder)
                .ThenBy(product => product.Id)
                .ToListAsync();

            var publishedProductIds = publishedProducts
                .Select(product => product.Id)
                .ToHashSet();

            if (publishedProductIds.Count != orderedProductIds.Count ||
                orderedProductIds.Any(id => !publishedProductIds.Contains(id)))
            {
                throw new ProductException(
                    "PRODUCT_ORDER_INVALID",
                    "Danh sách phải chứa đúng một lần tất cả Product đang đăng.",
                    "orderedProductIds");
            }

            var productsById = publishedProducts.ToDictionary(product => product.Id);
            var updatedAt = DateTimeOffset.UtcNow;

            for (var index = 0; index < orderedProductIds.Count; index++)
            {
                var product = productsById[orderedProductIds[index]];
                var displayOrder = index + 1;

                if (product.DisplayOrder == displayOrder)
                {
                    continue;
                }

                product.DisplayOrder = displayOrder;
                product.UpdatedAt = updatedAt;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return orderedProductIds
                .Select(id => ToListItemResponse(productsById[id]))
                .ToList();
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new ProductException(
                "PRODUCT_ORDER_CONFLICT",
                "Danh sách Product đang đăng đã thay đổi. Vui lòng tải lại và thử lại.",
                exception);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new ProductException(
                "PRODUCT_ORDER_UPDATE_FAILED",
                "Không thể lưu thứ tự Product.",
                exception);
        }
    }

    private static void ValidateRequest(Request.GetProductListRequest request)
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

        if (fields.Count > 0)
        {
            throw new ProductException(
                "PRODUCT_LIST_QUERY_INVALID",
                "Thông tin phân trang không hợp lệ.",
                fields.ToArray());
        }

        if (request.Search?.Trim().Length > 200)
        {
            throw new ProductException(
                "PRODUCT_LIST_QUERY_INVALID",
                "Từ khóa tìm kiếm không được vượt quá 200 ký tự.",
                "search");
        }
    }

    private static string? GetSummary(VNZ.Repository.Entity.Json.ProductContent? content)
    {
        return content?.Blocks
            .Where(block => block.Type == ContentBlockType.Description &&
                            !string.IsNullOrWhiteSpace(block.Text))
            .OrderBy(block => block.Order)
            .Select(block => block.Text)
            .FirstOrDefault();
    }

    private static Response.ProductListItemResponse ToListItemResponse(Product product)
    {
        return new Response.ProductListItemResponse
        {
            Id = product.Id,
            Name = product.Name,
            LogoUrl = product.LogoUrl,
            ProductUrl = product.ProductUrl,
            Summary = GetSummary(product.Content),
            Status = product.Status,
            IsPublished = product.IsPublished,
            DisplayOrder = product.DisplayOrder,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException postgresException)
            {
                return postgresException;
            }

            exception = exception.InnerException!;
        }

        return null;
    }
}
