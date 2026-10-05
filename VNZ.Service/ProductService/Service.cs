using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;
using MediaService = VNZ.Service.Utils.MediaService;
using RichTextService = VNZ.Service.Utils.RichTextService;

namespace VNZ.Service.ProductService;

public sealed class Service : IService
{
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

    public async Task<Response.DeleteProductResponse> DeleteProductAsync(Guid id)
    {
        var affectedRows = await _dbContext.Products
            .Where(product => product.Id == id && !product.IsDelete && !product.IsPublished)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(product => product.IsDelete, true));

        if (affectedRows == 1)
        {
            return new Response.DeleteProductResponse { Id = id };
        }

        var productExists = await _dbContext.Products
            .AsNoTracking()
            .AnyAsync(product => product.Id == id && !product.IsDelete);

        if (!productExists)
        {
            throw new ProductException("PRODUCT_NOT_FOUND", "Không tìm thấy Product.");
        }

        throw new ProductException(
            "PRODUCT_DELETE_FORBIDDEN",
            "Không thể xóa Product đang hiển thị trên website.");
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

        var content = SanitizeProductContent(request.Content);
        ValidateProductContent(content);

        var logoUrl = await UploadProductImageIfPresentAsync(request.Logo);
        var wordmarkUrl = await UploadProductImageIfPresentAsync(request.Wordmark);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            LogoUrl = logoUrl,
            WordmarkUrl = wordmarkUrl,
            ProductUrl = request.ProductUrl,
            Content = ToProductContent(content),
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

        try
        {
            if (request.IsPublished.HasValue)
            {
                ValidateStatusRequest(request);
                return await UpdateProductPublicationStateAsync(id, request.IsPublished.Value);
            }

            return await UpdateProductProfileAsync(id, request);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new ProductException(
                "PRODUCT_OPERATION_FAILED",
                "Không thể cập nhật Product.",
                exception);
        }
    }

    private async Task<Response.ProductDetailResponse> UpdateProductProfileAsync(
        Guid id,
        Request.UpdateProductRequest request)
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

        if (product.IsPublished)
        {
            throw new ProductException(
                "PRODUCT_PUBLISHED_CANNOT_EDIT",
                "Product đang được đăng. Hãy gỡ đăng trước khi chỉnh sửa.",
                "isPublished");
        }

        var logoAction = NormalizeProductImageAction(request.LogoAction);
        var wordmarkAction = NormalizeProductImageAction(request.WordmarkAction);
        ValidateProductImageActions(logoAction, wordmarkAction, request);

        var content = SanitizeProductContent(request.Content);
        ValidateUpdateRequest(request, content);

        var updatedAt = DateTimeOffset.UtcNow;
        var currentLogoUrl = NormalizeProductImageUrl(product.LogoUrl);
        var currentWordmarkUrl = NormalizeProductImageUrl(product.WordmarkUrl);
        var removeLogo = string.Equals(logoAction, "remove", StringComparison.Ordinal);
        var removeWordmark = string.Equals(wordmarkAction, "remove", StringComparison.Ordinal);

        var logoUrl = removeLogo
            ? null
            : request.Logo is null
                ? currentLogoUrl
                : await UploadProductImageIfPresentAsync(request.Logo);

        var wordmarkUrl = removeWordmark
            ? null
            : request.Wordmark is null
                ? currentWordmarkUrl
                : await UploadProductImageIfPresentAsync(request.Wordmark);

        product.Name = request.Name!.Trim();
        product.LogoUrl = logoUrl;
        product.WordmarkUrl = wordmarkUrl;
        product.ProductUrl = request.ProductUrl;
        product.Content = ToProductContent(content);
        product.Status = request.Status!.Value;
        product.IsPublished = false;
        product.DisplayOrder = null;
        product.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToDetailResponse(product);
    }

    private async Task<Response.ProductDetailResponse> UpdateProductPublicationStateAsync(
        Guid id,
        bool isPublished)
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

        if (product.IsPublished == isPublished)
        {
            if (isPublished)
            {
                ValidatePublishableProduct(product);
            }

            await transaction.CommitAsync();
            return ToDetailResponse(product);
        }

        var updatedAt = DateTimeOffset.UtcNow;

        if (isPublished)
        {
            ValidatePublishableProduct(product);

            var lastDisplayOrder = await _dbContext.Products
                .Where(item => item.IsPublished)
                .MaxAsync(item => (int?)item.DisplayOrder) ?? 0;

            product.IsPublished = true;
            product.DisplayOrder = lastDisplayOrder + 1;
        }
        else
        {
            var publishedProducts = await GetPublishedProductsAsync(product.Id);

            product.IsPublished = false;
            product.DisplayOrder = null;
            NormalizePublishedProductDisplayOrders(publishedProducts, updatedAt);
        }

        product.UpdatedAt = updatedAt;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return ToDetailResponse(product);
    }

    private async Task<List<Product>> GetPublishedProductsAsync(Guid excludedProductId)
    {
        return await _dbContext.Products
            .Where(product => product.IsPublished && product.Id != excludedProductId)
            .OrderBy(product => product.DisplayOrder == null)
            .ThenBy(product => product.DisplayOrder)
            .ThenBy(product => product.CreatedAt)
            .ThenBy(product => product.Id)
            .ToListAsync();
    }

    private static void NormalizePublishedProductDisplayOrders(
        List<Product> publishedProducts,
        DateTimeOffset updatedAt)
    {
        for (var index = 0; index < publishedProducts.Count; index++)
        {
            var displayOrder = index + 1;
            var product = publishedProducts[index];

            if (product.DisplayOrder == displayOrder)
            {
                continue;
            }

            product.DisplayOrder = displayOrder;
            product.UpdatedAt = updatedAt;
        }
    }

    private static void ValidatePublishableProduct(Product product)
    {
        if (product.Status != ProductStatus.Completed)
        {
            throw new ProductException(
                "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH",
                "Chỉ sản phẩm đã hoàn thành mới có thể đăng.",
                "status",
                "isPublished");
        }

        if (NormalizeProductImageUrl(product.LogoUrl) is null)
        {
            throw ProductImagesRequired("logoUrl");
        }

        if (NormalizeProductImageUrl(product.WordmarkUrl) is null)
        {
            throw ProductImagesRequired("wordmarkUrl");
        }
    }

    public async Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequest(request);

        var search = request.Search?.Trim();
        var statuses = ParseProductStatuses(request.Status);

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

            if (statuses.Count > 0)
            {
                query = query.Where(product => statuses.Contains(product.Status));
            }

            if (request.IsPublished.HasValue)
            {
                query = query.Where(product => product.IsPublished == request.IsPublished.Value);
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
                .Select(product => ToListItemResponse(product))
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

    public async Task<Response.PublicProductListResponse> GetPublicProductListAsync()
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Status == ProductStatus.Completed &&
                product.IsPublished &&
                product.DisplayOrder.HasValue &&
                product.LogoUrl != null &&
                product.WordmarkUrl != null)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Id)
            .ToListAsync();

        var items = products
            .Select(product => new Response.PublicProductListItemResponse
            {
                LogoUrl = product.LogoUrl,
                WordmarkUrl = product.WordmarkUrl,
                Content = product.Content is null
                    ? null
                    : new Response.PublicProductContentResponse
                    {
                        Blocks = product.Content.Blocks
                            .OrderBy(block => block.Order)
                            .Select(block => new Response.PublicContentBlockResponse
                            {
                                Type = block.Type,
                                Order = block.Order,
                                Text = block.Text,
                                Items = block.Items is null
                                    ? null
                                    : block.Items
                                        .Select(item => new Response.PublicFeatureItemResponse
                                        {
                                            Title = item.Title
                                        })
                                        .ToList()
                            })
                            .ToList()
                    },
                ProductUrl = product.ProductUrl
            })
            .ToList();

        return new Response.PublicProductListResponse
        {
            Items = items
        };
    }

    public async Task<List<Response.ProductOrderItemResponse>> GetOrderableProductsAsync()
    {
        try
        {
            return await _dbContext.Products
                .AsNoTracking()
                .Where(product => product.IsPublished)
                .OrderBy(product => product.DisplayOrder)
                .Select(product => new Response.ProductOrderItemResponse
                {
                    Id = product.Id,
                    Name = product.Name,
                    LogoUrl = product.LogoUrl,
                    DisplayOrder = product.DisplayOrder!.Value
                })
                .ToListAsync();
        }
        catch (Exception exception) when (exception is not ProductException &&
                                          exception is not OperationCanceledException)
        {
            throw new ProductException(
                "PRODUCT_LIST_READ_FAILED",
                "Không thể đọc danh sách sản phẩm.",
                exception);
        }
    }

    public async Task<List<Response.ProductOrderItemResponse>> ReorderProductsAsync(
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
                .Select(id => ToProductOrderItemResponse(productsById[id]))
                .ToList();
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new ProductException(
                "PRODUCT_ORDER_CONFLICT",
                "Thứ tự sản phẩm đã thay đổi. Vui lòng tải lại danh sách.",
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

    private async Task<string> UploadProductImageAsync(IFormFile file)
    {
        var uploadResult = await _mediaService.UploadImageAsync(
            new MediaService.Request.UploadImageRequest
            {
                File = file,
                Purpose = "ProductLogo"
            });

        return uploadResult.Url;
    }

    private async Task<string?> UploadProductImageIfPresentAsync(IFormFile? file)
    {
        if (file is null)
        {
            return null;
        }

        return await UploadProductImageAsync(file);
    }

    private static string? NormalizeProductImageUrl(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl)
            ? null
            : imageUrl.Trim();
    }

    private static string? NormalizeProductImageAction(string? action)
    {
        return string.IsNullOrWhiteSpace(action)
            ? null
            : action.Trim();
    }

    private static void ValidateProductImageActions(
        string? logoAction,
        string? wordmarkAction,
        Request.UpdateProductRequest request)
    {
        if (logoAction is not null &&
            !string.Equals(logoAction, "remove", StringComparison.Ordinal))
        {
            throw new ProductException(
                "PRODUCT_IMAGE_ACTION_INVALID",
                "Thao tác Logo của Product không hợp lệ.",
                "logoAction");
        }

        if (wordmarkAction is not null &&
            !string.Equals(wordmarkAction, "remove", StringComparison.Ordinal))
        {
            throw new ProductException(
                "PRODUCT_IMAGE_ACTION_INVALID",
                "Thao tác Wordmark của Product không hợp lệ.",
                "wordmarkAction");
        }

        if (string.Equals(logoAction, "remove", StringComparison.Ordinal) &&
            request.Logo is not null)
        {
            throw new ProductException(
                "PRODUCT_IMAGE_ACTION_INVALID",
                "Không thể vừa gỡ Logo vừa gửi Logo mới.",
                "logoAction",
                "logo");
        }

        if (string.Equals(wordmarkAction, "remove", StringComparison.Ordinal) &&
            request.Wordmark is not null)
        {
            throw new ProductException(
                "PRODUCT_IMAGE_ACTION_INVALID",
                "Không thể vừa gỡ Wordmark vừa gửi Wordmark mới.",
                "wordmarkAction",
                "wordmark");
        }
    }

    private static ProductException ProductImagesRequired(params string[] fields)
    {
        return new ProductException(
            "PRODUCT_IMAGES_REQUIRED",
            "Product đăng công khai phải có cả Logo và Wordmark.",
            fields);
    }

    private static void ValidateRequest(Request.GetProductListRequest request)
    {
        var fields = new List<string>();

        if (request.Page < 1)
        {
            fields.Add("page");
        }

        if (request.PageSize is not (10 or 20 or 50))
        {
            fields.Add("pageSize");
        }

        if (request.Search?.Trim().Length > 200)
        {
            fields.Add("search");
        }

        if (request.Status is not null && request.Status.Any(status =>
                !string.IsNullOrWhiteSpace(status) &&
                !string.Equals(status.Trim(), nameof(ProductStatus.InProgress), StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status.Trim(), nameof(ProductStatus.Completed), StringComparison.OrdinalIgnoreCase)))
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

    private static List<ProductStatus> ParseProductStatuses(List<string>? statuses)
    {
        if (statuses is null)
        {
            return [];
        }

        return statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => string.Equals(
                status.Trim(),
                nameof(ProductStatus.InProgress),
                StringComparison.OrdinalIgnoreCase)
                ? ProductStatus.InProgress
                : ProductStatus.Completed)
            .Distinct()
            .ToList();
    }

    private static void ValidateStatusRequest(Request.UpdateProductRequest request)
    {
        if (HasProductProfilePayload(request))
        {
            throw new ProductException(
                "PRODUCT_VALIDATION_ERROR",
                "Request đăng/gỡ đăng không được kèm dữ liệu hồ sơ.",
                "isPublished");
        }
    }

    private static bool HasProductProfilePayload(Request.UpdateProductRequest request)
    {
        return request.Name is not null ||
            request.Logo is not null ||
            request.Wordmark is not null ||
            request.LogoAction is not null ||
            request.WordmarkAction is not null ||
            request.ProductUrl is not null ||
            request.Content is not null ||
            request.Status.HasValue;
    }

    private static void ValidateUpdateRequest(
        Request.UpdateProductRequest request,
        Request.ProductContentRequest? content)
    {
        var fields = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            fields.Add("name");
        }

        if (!request.Status.HasValue || !Enum.IsDefined(request.Status.Value))
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

        ValidateProductContent(content);
    }

    private Request.ProductContentRequest? SanitizeProductContent(
        Request.ProductContentRequest? content)
    {
        if (content is null)
        {
            return null;
        }

        if (content.Blocks is null)
        {
            return new Request.ProductContentRequest
            {
                Blocks = null!
            };
        }

        return new Request.ProductContentRequest
        {
            Blocks = content.Blocks
                .Select(block => new Request.ContentBlockRequest
                {
                    Id = block.Id,
                    Type = block.Type,
                    Order = block.Order,
                    Text = block.Type == ContentBlockType.Feature
                        ? null
                        : _richTextService.Sanitize(block.Text?.Trim(), allowLinks: true),
                    Items = block.Items?
                        .Select(item => new Request.FeatureItemRequest
                        {
                            Id = item.Id,
                            Title = SanitizePlainText(item.Title)
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private string? SanitizePlainText(string? value)
    {
        var sanitized = _richTextService.Sanitize(value?.Trim(), allowLinks: false);
        return sanitized is null
            ? null
            : _richTextService.ToPlainText(sanitized).Trim();
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

    private Response.ProductListItemResponse ToListItemResponse(Product product)
    {
        var summary = GetSummary(product.Content);

        return new Response.ProductListItemResponse
        {
            Id = product.Id,
            Name = product.Name,
            LogoUrl = product.LogoUrl,
            ProductUrl = product.ProductUrl,
            Summary = summary is null
                ? null
                : _richTextService.ToPlainText(summary).Trim(),
            Status = product.Status,
            IsPublished = product.IsPublished,
            DisplayOrder = product.DisplayOrder,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            CanDelete = !product.IsPublished,
            DeleteBlockedReason = product.IsPublished ? "PUBLIC_VISIBLE" : null
        };
    }

    private static Response.ProductOrderItemResponse ToProductOrderItemResponse(Product product)
    {
        return new Response.ProductOrderItemResponse
        {
            Id = product.Id,
            Name = product.Name,
            LogoUrl = product.LogoUrl,
            DisplayOrder = product.DisplayOrder!.Value
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
            WordmarkUrl = product.WordmarkUrl,
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
            UpdatedAt = product.UpdatedAt,
            CanDelete = !product.IsPublished,
            DeleteBlockedReason = product.IsPublished ? "PUBLIC_VISIBLE" : null
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
