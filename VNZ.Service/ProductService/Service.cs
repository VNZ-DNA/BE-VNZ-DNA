using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;

namespace VNZ.Service.ProductService;

public sealed class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.ProductDetailResponse> CreateProductAsync(
        Request.CreateProductRequest request,
        Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new ProductException("PRODUCT_VALIDATION_FAILED", "Tên Product là bắt buộc.", "name");
        }

        ValidateProductContent(request.Content);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            LogoUrl = request.LogoUrl,
            ProductUrl = request.ProductUrl,
            Content = ToProductContent(request.Content),
            Status = ProductStatus.InProgress,
            IsPublished = false,
            DisplayOrder = null,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new ProductException("PRODUCT_CREATE_FAILED", "Không thể tạo Product.", exception);
        }
    }

    public async Task<Response.ProductDetailResponse> GetProductDetailAsync(Guid id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id);

        if (product is null)
        {
            throw new ProductException(
                "PRODUCT_NOT_FOUND",
                "Không tìm thấy Product.");
        }

        return ToDetailResponse(product);
    }

    public async Task<Response.ProductDetailResponse> UpdateProductAsync(
        Guid id,
        Request.UpdateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateUpdateRequest(request);

        if (request.IsPublished && request.Status == ProductStatus.InProgress)
        {
            throw new ProductException(
                "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH",
                "Chỉ sản phẩm đã hoàn thành mới có thể đăng.",
                "status",
                "isPublished");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var product = await _dbContext.Products
                .FirstOrDefaultAsync(product => product.Id == id);

            if (product is null)
            {
                throw new ProductException(
                    "PRODUCT_NOT_FOUND",
                    "Không tìm thấy Product.");
            }

            var updatedAt = DateTimeOffset.UtcNow;

            if (!product.IsPublished && request.IsPublished)
            {
                var lastDisplayOrder = await _dbContext.Products
                    .Where(item => item.IsPublished)
                    .MaxAsync(item => (int?)item.DisplayOrder) ?? 0;

                product.DisplayOrder = lastDisplayOrder + 1;
            }
            else if (product.IsPublished && !request.IsPublished)
            {
                product.DisplayOrder = null;

                var publishedProducts = await _dbContext.Products
                    .Where(item => item.IsPublished && item.Id != product.Id)
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.CreatedAt)
                    .ThenBy(item => item.Id)
                    .ToListAsync();

                for (var index = 0; index < publishedProducts.Count; index++)
                {
                    var publishedProduct = publishedProducts[index];
                    var displayOrder = index + 1;

                    if (publishedProduct.DisplayOrder == displayOrder)
                    {
                        continue;
                    }

                    publishedProduct.DisplayOrder = displayOrder;
                    publishedProduct.UpdatedAt = updatedAt;
                }
            }

            product.Name = request.Name.Trim();
            product.LogoUrl = request.LogoUrl;
            product.ProductUrl = request.ProductUrl;
            product.Content = ToProductContent(request.Content);
            product.Status = request.Status;
            product.IsPublished = request.IsPublished;
            product.UpdatedAt = updatedAt;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new ProductException(
                "PRODUCT_OPERATION_FAILED",
                "Không thể cập nhật Product.",
                exception);
        }
    }

    public async Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var search = request.Search?.Trim();
        var status = ParseProductStatus(request.Status);

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

            if (status.HasValue)
            {
                query = query.Where(product => product.Status == status.Value);
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

        if (request.Search?.Trim().Length > 200)
        {
            fields.Add("search");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            !string.Equals(request.Status.Trim(), nameof(ProductStatus.InProgress), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status.Trim(), nameof(ProductStatus.Completed), StringComparison.OrdinalIgnoreCase))
        {
            fields.Add("status");
        }

        if (fields.Count > 0)
        {
            throw new ProductException(
                "PRODUCT_LIST_QUERY_INVALID",
                "Thông tin truy vấn danh sách Product không hợp lệ.",
                fields.ToArray());
        }
    }

    private static ProductStatus? ParseProductStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return string.Equals(status.Trim(), nameof(ProductStatus.InProgress), StringComparison.OrdinalIgnoreCase)
            ? ProductStatus.InProgress
            : ProductStatus.Completed;
    }

    private static void ValidateUpdateRequest(Request.UpdateProductRequest request)
    {
        var fields = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            fields.Add("name");
        }

        if (!Enum.IsDefined(request.Status))
        {
            fields.Add("status");
        }

        if (fields.Count > 0)
        {
            throw new ProductException(
                "PRODUCT_VALIDATION_ERROR",
                "Thông tin Product không hợp lệ.",
                fields.ToArray());
        }

        ValidateProductContent(request.Content);
    }

    private static void ValidateProductContent(Request.ProductContentRequest? content)
    {
        if (content is null)
        {
            return;
        }

        var blocks = content.Blocks;
        if (blocks is null)
        {
            throw new ProductException(
                "PRODUCT_CONTENT_INVALID",
                "Nội dung Product không hợp lệ.",
                "content.blocks");
        }

        var blockIds = new HashSet<Guid>();
        var orders = new HashSet<int>();

        foreach (var block in blocks)
        {
            if (block.Id == Guid.Empty || !blockIds.Add(block.Id))
            {
                throw new ProductException(
                    "PRODUCT_CONTENT_INVALID",
                    "Nội dung Product không hợp lệ.",
                    "content.blocks.id");
            }

            if (!Enum.IsDefined(block.Type) || !orders.Add(block.Order))
            {
                throw new ProductException(
                    "PRODUCT_CONTENT_INVALID",
                    "Nội dung Product không hợp lệ.",
                    "content.blocks");
            }

            if (block.Type == ContentBlockType.Feature)
            {
                if (block.Text is not null || block.Items is null)
                {
                    throw new ProductException(
                        "PRODUCT_CONTENT_INVALID",
                        "Nội dung Product không hợp lệ.",
                        "content.blocks");
                }

                var itemIds = new HashSet<Guid>();
                foreach (var item in block.Items)
                {
                    if (item.Id == Guid.Empty || !itemIds.Add(item.Id) || item.Title is null)
                    {
                        throw new ProductException(
                            "PRODUCT_CONTENT_INVALID",
                            "Nội dung Product không hợp lệ.",
                            "content.blocks.items");
                    }
                }
            }
            else if (block.Text is null || block.Items is not null)
            {
                throw new ProductException(
                    "PRODUCT_CONTENT_INVALID",
                    "Nội dung Product không hợp lệ.",
                    "content.blocks");
            }
        }

        for (var order = 1; order <= blocks.Count; order++)
        {
            if (!orders.Contains(order))
            {
                throw new ProductException(
                    "PRODUCT_CONTENT_INVALID",
                    "Nội dung Product không hợp lệ.",
                    "content.blocks.order");
            }
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

    private static ProductContent? ToProductContent(Request.ProductContentRequest? content)
    {
        if (content is null)
        {
            return null;
        }

        return new ProductContent
        {
            Blocks = content.Blocks.Select(block => new ContentBlock
            {
                Id = block.Id,
                Type = block.Type,
                Order = block.Order,
                Text = block.Text,
                Items = block.Items?.Select(item => new FeatureItem
                {
                    Id = item.Id,
                    Title = item.Title
                }).ToList()
            }).ToList()
        };
    }

    private static Response.ProductDetailResponse ToDetailResponse(Product product)
    {
        return new Response.ProductDetailResponse
        {
            Id = product.Id,
            Name = product.Name,
            LogoUrl = product.LogoUrl,
            ProductUrl = product.ProductUrl,
            Content = product.Content is null
                ? null
                : new Response.ProductContentResponse
                {
                    Blocks = product.Content.Blocks
                        .OrderBy(block => block.Order)
                        .Select(block => new Response.ContentBlockResponse
                        {
                            Id = block.Id,
                            Type = block.Type,
                            Order = block.Order,
                            Text = block.Text,
                            Items = block.Items?.Select(item => new Response.FeatureItemResponse
                            {
                                Id = item.Id,
                                Title = item.Title
                            }).ToList()
                        }).ToList()
                },
            Status = product.Status,
            IsPublished = product.IsPublished,
            DisplayOrder = product.DisplayOrder,
            CreatedBy = product.CreatedBy,
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
