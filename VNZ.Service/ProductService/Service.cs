using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;
using VNZ.Service.Localization;
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
        var translations = SanitizeProductTranslations(request.Translations);

        var uploadedImages = new List<UploadedProductImage>();

        try
        {
            // Media is uploaded before the short database transaction. Any
            // uploaded asset is tracked so a failed DB write can clean it up.
            var logo = await UploadProductImageResponseIfPresentAsync(request.Logo);
            AddUploadedImage(uploadedImages, logo);

            var wordmark = await UploadProductImageResponseIfPresentAsync(request.Wordmark);
            AddUploadedImage(uploadedImages, wordmark);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                LogoUrl = logo?.Url,
                WordmarkUrl = wordmark?.Url,
                ProductUrl = request.ProductUrl,
                Content = ToProductContent(content),
                Translations = translations,
                Status = ProductStatus.InProgress,
                IsPublished = false,
                DisplayOrder = null,
                CreatedBy = createdBy,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null
            };

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw new ProductException("PRODUCT_CREATE_FAILED", "Không thể tạo Product.", exception);
        }
        catch
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw;
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

        var current = await _dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id);

        if (current is null)
        {
            throw new ProductException(
                "PRODUCT_NOT_FOUND",
                "KhÃ´ng tÃ¬m tháº¥y Product.");
        }

        EnsureExpectedUpdatedAt(request.ExpectedUpdatedAt, current.UpdatedAt);

        // A published Product has a state-only PUT contract. It must be
        // unpublished before any content or media can be replaced.
        if (current.IsPublished)
        {
            if (request.IsPublished != false)
            {
                throw new ProductException(
                    "PRODUCT_PUBLISHED_CANNOT_EDIT",
                    "Product Ä‘ang Ä‘Æ°á»£c Ä‘Äƒng. HÃ£y gá»¡ Ä‘Äƒng trÆ°á»›c khi chá»‰nh sá»­a.",
                    "isPublished");
            }

            return await UnpublishProductAsync(id, request.ExpectedUpdatedAt);
        }

        // All request validation and rich-content normalization happen before
        // opening a transaction. The transaction below is intentionally kept
        // short and only re-reads the row, checks the concurrency token, and
        // writes the replacement.
        var logoAction = NormalizeProductImageAction(request.LogoAction);
        var wordmarkAction = NormalizeProductImageAction(request.WordmarkAction);
        ValidateProductImageActions(logoAction, wordmarkAction, request);

        var isPublished = request.IsPublished.GetValueOrDefault();
        var content = SanitizeProductContent(request.Content);
        ValidateUpdateRequest(request, content, isPublished);
        var translations = SanitizeProductTranslations(request.Translations);

        if (isPublished && request.Status == ProductStatus.InProgress)
        {
            throw new ProductException(
                "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH",
                "Chá»‰ sáº£n pháº©m Ä‘Ã£ hoÃ n thÃ nh má»›i cÃ³ thá»ƒ Ä‘Äƒng.",
                "status",
                "isPublished");
        }

        if (isPublished)
        {
            ValidateProductBilingualContent(content, translations?.En?.Content);
        }

        var currentLogoUrl = NormalizeProductImageUrl(current.LogoUrl);
        var currentWordmarkUrl = NormalizeProductImageUrl(current.WordmarkUrl);
        var removeLogo = string.Equals(logoAction, "remove", StringComparison.Ordinal);
        var removeWordmark = string.Equals(wordmarkAction, "remove", StringComparison.Ordinal);
        var hasFinalLogo = !removeLogo && (request.Logo is not null || currentLogoUrl is not null);
        var hasFinalWordmark = !removeWordmark &&
            (request.Wordmark is not null || currentWordmarkUrl is not null);

        if (isPublished && !hasFinalLogo)
        {
            throw ProductImagesRequired("logoUrl");
        }

        if (isPublished && !hasFinalWordmark)
        {
            throw ProductImagesRequired("wordmarkUrl");
        }

        var uploadedImages = new List<UploadedProductImage>();

        try
        {
            var logo = request.Logo is null
                ? null
                : await UploadProductImageResponseAsync(request.Logo);
            AddUploadedImage(uploadedImages, logo);

            var wordmark = request.Wordmark is null
                ? null
                : await UploadProductImageResponseAsync(request.Wordmark);
            AddUploadedImage(uploadedImages, wordmark);

            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var product = await _dbContext.Products
                .SingleOrDefaultAsync(item => item.Id == id);

            if (product is null)
            {
                throw new ProductException("PRODUCT_NOT_FOUND", "KhÃ´ng tÃ¬m tháº¥y Product.");
            }

            EnsureExpectedUpdatedAt(request.ExpectedUpdatedAt, product.UpdatedAt);

            if (product.IsPublished)
            {
                throw new ProductException(
                    "PRODUCT_PUBLISHED_CANNOT_EDIT",
                    "Product Ä‘ang Ä‘Æ°á»£c Ä‘Äƒng. HÃ£y gá»¡ Ä‘Äƒng trÆ°á»›c khi chá»‰nh sá»­a.",
                    "isPublished");
            }

            var updatedAt = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();
            var finalLogoUrl = removeLogo
                ? null
                : logo?.Url ?? NormalizeProductImageUrl(product.LogoUrl);
            var finalWordmarkUrl = removeWordmark
                ? null
                : wordmark?.Url ?? NormalizeProductImageUrl(product.WordmarkUrl);

            if (isPublished && string.IsNullOrWhiteSpace(finalLogoUrl))
            {
                throw ProductImagesRequired("logoUrl");
            }

            if (isPublished && string.IsNullOrWhiteSpace(finalWordmarkUrl))
            {
                throw ProductImagesRequired("wordmarkUrl");
            }

            product.DisplayOrder = isPublished
                ? (await _dbContext.Products
                    .Where(item => item.IsPublished)
                    .MaxAsync(item => (int?)item.DisplayOrder) ?? 0) + 1
                : null;
            product.Name = request.Name.Trim();
            product.LogoUrl = finalLogoUrl;
            product.WordmarkUrl = finalWordmarkUrl;
            product.ProductUrl = request.ProductUrl;
            product.Content = ToProductContent(content);
            product.Translations = translations;
            product.Status = request.Status!.Value;
            product.IsPublished = isPublished;
            product.UpdatedAt = updatedAt;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (DbUpdateConcurrencyException)
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t bá»Ÿi má»™t yÃªu cáº§u khÃ¡c.",
                "expectedUpdatedAt");
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t bá»Ÿi má»™t yÃªu cáº§u khÃ¡c.",
                "expectedUpdatedAt");
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw new ProductException(
                "PRODUCT_OPERATION_FAILED",
                "KhÃ´ng thá»ƒ cáº­p nháº­t Product.",
                exception);
        }
        catch
        {
            await CleanupUploadedImagesAsync(uploadedImages);
            throw;
        }
    }

    private async Task<Response.ProductDetailResponse> UpdateProductLegacyAsync(
        Guid id,
        Request.UpdateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

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

            EnsureExpectedUpdatedAt(request.ExpectedUpdatedAt, product.UpdatedAt);

            var updatedAt = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();

            if (product.IsPublished)
            {
                if (request.IsPublished != false)
                {
                    throw new ProductException(
                        "PRODUCT_PUBLISHED_CANNOT_EDIT",
                        "Product đang được đăng. Hãy gỡ đăng trước khi chỉnh sửa.",
                        "isPublished");
                }

                // Khi Product đang đăng, PUT chỉ thực hiện gỡ đăng.
                // Các field khác trong request không được áp dụng cho đến request tiếp theo.
                product.IsPublished = false;
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

                product.UpdatedAt = updatedAt;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToDetailResponse(product);
            }

            var logoAction = NormalizeProductImageAction(request.LogoAction);
            var wordmarkAction = NormalizeProductImageAction(request.WordmarkAction);
            ValidateProductImageActions(logoAction, wordmarkAction, request);

            var isPublished = request.IsPublished.GetValueOrDefault();
            var content = SanitizeProductContent(request.Content);
            ValidateUpdateRequest(request, content, isPublished);
            var translations = SanitizeProductTranslations(request.Translations);

            if (isPublished && request.Status == ProductStatus.InProgress)
            {
                throw new ProductException(
                    "PRODUCT_IN_PROGRESS_CANNOT_PUBLISH",
                    "Chỉ sản phẩm đã hoàn thành mới có thể đăng.",
                    "status",
                    "isPublished");
            }

            if (isPublished)
            {
                ValidateProductBilingualContent(content, translations?.En?.Content);
            }

            var currentLogoUrl = NormalizeProductImageUrl(product.LogoUrl);
            var currentWordmarkUrl = NormalizeProductImageUrl(product.WordmarkUrl);

            var removeLogo = string.Equals(logoAction, "remove", StringComparison.Ordinal);
            var removeWordmark = string.Equals(wordmarkAction, "remove", StringComparison.Ordinal);
            var hasFinalLogo = !removeLogo &&
                (request.Logo is not null || currentLogoUrl is not null);
            var hasFinalWordmark = !removeWordmark &&
                (request.Wordmark is not null || currentWordmarkUrl is not null);

            if (isPublished && !hasFinalLogo)
            {
                throw ProductImagesRequired("logoUrl");
            }

            if (isPublished && !hasFinalWordmark)
            {
                throw ProductImagesRequired("wordmarkUrl");
            }

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

            if (isPublished)
            {
                var lastDisplayOrder = await _dbContext.Products
                    .Where(item => item.IsPublished)
                    .MaxAsync(item => (int?)item.DisplayOrder) ?? 0;

                product.DisplayOrder = lastDisplayOrder + 1;
            }
            else
            {
                product.DisplayOrder = null;
            }

            product.Name = request.Name.Trim();
            product.LogoUrl = logoUrl;
            product.WordmarkUrl = wordmarkUrl;
            product.ProductUrl = request.ProductUrl;
            product.Content = ToProductContent(content);
            product.Translations = translations;
            product.Status = request.Status!.Value;
            product.IsPublished = isPublished;
            product.UpdatedAt = updatedAt;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product đã được cập nhật bởi một yêu cầu khác.",
                "expectedUpdatedAt");
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product đã được cập nhật bởi một yêu cầu khác.",
                "expectedUpdatedAt");
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

    public async Task<Response.PublicProductListResponse> GetPublicProductListAsync(string? lang = null)
    {
        var resolvedLang = LocaleResolver.Resolve(lang);

        List<Product> products;

        try
        {
            products = await _dbContext.Products
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
        }
        catch (JsonException) when (resolvedLang == LocaleResolver.English)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Báº£n dá»‹ch tiáº¿ng Anh cá»§a Product khÃ´ng há»£p lá»‡.",
                "translations.en");
        }

        var items = products.Select(product =>
        {
            var content = resolvedLang == LocaleResolver.English
                ? ResolvePublicEnglishContent(product)
                : product.Content;

            return new Response.PublicProductListItemResponse
            {
                LogoUrl = product.LogoUrl,
                WordmarkUrl = product.WordmarkUrl,
                Content = ToPublicContentResponse(content),
                ProductUrl = product.ProductUrl
            };
        }).ToList();

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
            var updatedAt = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();

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

    private async Task<Response.ProductDetailResponse> UnpublishProductAsync(
        Guid id,
        DateTimeOffset? expectedUpdatedAt)
    {
        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var product = await _dbContext.Products
                .SingleOrDefaultAsync(item => item.Id == id);

            if (product is null)
            {
                throw new ProductException("PRODUCT_NOT_FOUND", "KhÃ´ng tÃ¬m tháº¥y Product.");
            }

            EnsureExpectedUpdatedAt(expectedUpdatedAt, product.UpdatedAt);

            if (!product.IsPublished)
            {
                throw new ProductException(
                    "CONTENT_CONFLICT",
                    "Product Ä‘Ã£ Ä‘Æ°á»£c thay Ä‘á»•i bá»Ÿi má»™t yÃªu cáº§u khÃ¡c.",
                    "expectedUpdatedAt");
            }

            product.IsPublished = false;
            product.DisplayOrder = null;
            product.UpdatedAt = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();

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
                publishedProduct.UpdatedAt = product.UpdatedAt;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToDetailResponse(product);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t bá»Ÿi má»™t yÃªu cáº§u khÃ¡c.",
                "expectedUpdatedAt");
        }
        catch (Exception exception) when (
            FindPostgresException(exception)?.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t bá»Ÿi má»™t yÃªu cáº§u khÃ¡c.",
                "expectedUpdatedAt");
        }
        catch (Exception exception) when (exception is DbUpdateException or PostgresException)
        {
            throw new ProductException(
                "PRODUCT_OPERATION_FAILED",
                "KhÃ´ng thá»ƒ gá»¡ Ä‘Äƒng Product.",
                exception);
        }
    }

    private async Task<UploadedProductImage?> UploadProductImageResponseIfPresentAsync(
        IFormFile? file)
    {
        if (file is null)
        {
            return null;
        }

        return await UploadProductImageResponseAsync(file);
    }

    private async Task<UploadedProductImage> UploadProductImageResponseAsync(IFormFile file)
    {
        var uploadResult = await _mediaService.UploadImageAsync(
            new MediaService.Request.UploadImageRequest
            {
                File = file,
                Purpose = "ProductLogo"
            });

        return new UploadedProductImage(uploadResult.Url, uploadResult.PublicId);
    }

    private static void AddUploadedImage(
        ICollection<UploadedProductImage> uploadedImages,
        UploadedProductImage? image)
    {
        if (image is not null)
        {
            uploadedImages.Add(image);
        }
    }

    private async Task CleanupUploadedImagesAsync(
        IEnumerable<UploadedProductImage> uploadedImages)
    {
        if (_mediaService is not MediaService.IAssetCleanupService cleanupService)
        {
            return;
        }

        foreach (var image in uploadedImages)
        {
            await cleanupService.DeleteImageAsync(image.PublicId);
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

    private static void EnsureExpectedUpdatedAt(
        DateTimeOffset? expectedUpdatedAt,
        DateTimeOffset? actualUpdatedAt)
    {
        if (expectedUpdatedAt.HasValue && expectedUpdatedAt != actualUpdatedAt)
        {
            throw new ProductException(
                "CONTENT_CONFLICT",
                "Product đã được cập nhật bởi một yêu cầu khác.",
                "expectedUpdatedAt");
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

    private static void ValidateUpdateRequest(
        Request.UpdateProductRequest request,
        Request.ProductContentRequest? content,
        bool isPublished)
    {
        var fields = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
        {
            fields.Add("name");
        }

        if (!request.IsPublished.HasValue)
        {
            fields.Add("isPublished");
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
                    // Multipart binding may omit `items` when a Feature block
                    // is intentionally empty. Normalize that transport shape
                    // to [] before graph validation and persistence.
                    Items = block.Type == ContentBlockType.Feature
                        ? (block.Items ?? new List<Request.FeatureItemRequest>())
                            .Select(item => new Request.FeatureItemRequest
                            {
                                Id = item.Id,
                                Title = SanitizePlainText(item.Title)
                            })
                            .ToList()
                        : block.Items?
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
                    if (item.Id == Guid.Empty || !itemIds.Add(item.Id))
                    {
                        throw new ProductException(
                            "PRODUCT_CONTENT_INVALID",
                            "Nội dung Product không hợp lệ.",
                            "content.blocks.items");
                    }
                }
            }
            else if (block.Items is not null)
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

    private ProductTranslations? SanitizeProductTranslations(
        Request.ProductTranslationsRequest? translations)
    {
        if (translations is null)
        {
            return null;
        }

        if (translations.En is null)
        {
            return new ProductTranslations();
        }

        Request.ProductContentRequest? content;

        try
        {
            content = SanitizeProductContent(translations.En.Content);
            ValidateProductContent(content);
        }
        catch (ProductException exception) when (exception.Code == "PRODUCT_CONTENT_INVALID")
        {
            throw new ProductException(
                "BILINGUAL_SCHEMA_INVALID",
                "Cấu trúc nội dung Product tiếng Anh không hợp lệ.",
                exception.Fields.ToArray());
        }

        return new ProductTranslations
        {
            En = new ProductEnglishTranslation
            {
                Content = ToProductContent(content)
            }
        };
    }

    private static void ValidateProductBilingualContent(
        Request.ProductContentRequest? vietnameseRequest,
        ProductContent? englishContent)
    {
        var vietnameseContent = ToProductContent(vietnameseRequest);

        if (vietnameseContent is null && englishContent is null)
        {
            return;
        }

        if (vietnameseContent is null)
        {
            throw new ProductException(
                "BILINGUAL_CONTENT_REQUIRED",
                "Product cần có nội dung tiếng Việt khi Publish.",
                "content");
        }

        if (englishContent is null)
        {
            throw new ProductException(
                "BILINGUAL_CONTENT_REQUIRED",
                "Product cần có nội dung tiếng Anh khi Publish.",
                "translations.en.content");
        }

        if (!HasSameProductContentShape(vietnameseContent, englishContent))
        {
            throw new ProductException(
                "BILINGUAL_SCHEMA_INVALID",
                "Cấu trúc nội dung Product tiếng Anh phải khớp nội dung tiếng Việt.",
                "translations.en.content");
        }

        var missingFields = new List<string>();

        for (var blockIndex = 0; blockIndex < vietnameseContent.Blocks.Count; blockIndex++)
        {
            var vietnameseBlock = vietnameseContent.Blocks[blockIndex];
            var englishBlock = englishContent.Blocks[blockIndex];
            var blockField = $"translations.en.content.blocks[{blockIndex}]";

            if (vietnameseBlock.Type == ContentBlockType.Feature)
            {
                for (var itemIndex = 0; itemIndex < vietnameseBlock.Items!.Count; itemIndex++)
                {
                    if (string.IsNullOrWhiteSpace(vietnameseBlock.Items[itemIndex].Title))
                    {
                        missingFields.Add($"content.blocks[{blockIndex}].items[{itemIndex}].title");
                    }

                    if (string.IsNullOrWhiteSpace(englishBlock.Items![itemIndex].Title))
                    {
                        missingFields.Add(
                            $"{blockField}.items[{itemIndex}].title");
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(vietnameseBlock.Text))
                {
                    missingFields.Add($"content.blocks[{blockIndex}].text");
                }

                if (string.IsNullOrWhiteSpace(englishBlock.Text))
                {
                    missingFields.Add($"{blockField}.text");
                }
            }
        }

        if (missingFields.Count > 0)
        {
            throw new ProductException(
                "BILINGUAL_CONTENT_REQUIRED",
                "Product cần đủ nội dung tiếng Việt và tiếng Anh để Publish.",
                missingFields.ToArray());
        }
    }

    private static bool HasSameProductContentShape(
        ProductContent vietnameseContent,
        ProductContent englishContent)
    {
        if (vietnameseContent.Blocks.Count != englishContent.Blocks.Count)
        {
            return false;
        }

        for (var blockIndex = 0; blockIndex < vietnameseContent.Blocks.Count; blockIndex++)
        {
            var vietnameseBlock = vietnameseContent.Blocks[blockIndex];
            var englishBlock = englishContent.Blocks[blockIndex];

            if (vietnameseBlock.Id != englishBlock.Id ||
                vietnameseBlock.Type != englishBlock.Type ||
                vietnameseBlock.Order != englishBlock.Order)
            {
                return false;
            }

            if (vietnameseBlock.Type != ContentBlockType.Feature)
            {
                continue;
            }

            if (vietnameseBlock.Items is null || englishBlock.Items is null ||
                vietnameseBlock.Items.Count != englishBlock.Items.Count)
            {
                return false;
            }

            for (var itemIndex = 0; itemIndex < vietnameseBlock.Items.Count; itemIndex++)
            {
                if (vietnameseBlock.Items[itemIndex].Id != englishBlock.Items[itemIndex].Id)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static ProductContent? ResolvePublicEnglishContent(Product product)
    {
        var englishContent = product.Translations?.En?.Content;

        if (product.Content is null && englishContent is null)
        {
            return null;
        }

        if (product.Content is null || englishContent is null)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Anh của Product đang bị thiếu hoặc sai cấu trúc.",
                product.Content is null ? "content" : "translations.en.content");
        }

        if (!HasSameProductContentShape(product.Content, englishContent) ||
            !HasCompleteProductContent(product.Content) ||
            !HasCompleteProductContent(englishContent))
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Anh của Product đang bị thiếu hoặc sai cấu trúc.",
                "translations.en.content");
        }

        return englishContent;
    }

    private static bool HasCompleteProductContent(ProductContent content)
    {
        return content.Blocks.All(block => block.Type == ContentBlockType.Feature
            ? block.Items is not null && block.Items.All(item => !string.IsNullOrWhiteSpace(item.Title))
            : !string.IsNullOrWhiteSpace(block.Text));
    }

    private static Response.PublicProductContentResponse? ToPublicContentResponse(
        ProductContent? content)
    {
        if (content is null)
        {
            return null;
        }

        return new Response.PublicProductContentResponse
        {
            Blocks = content.Blocks
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
        };
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
            UpdatedAt = product.UpdatedAt
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
            Content = ToDetailContentResponse(product.Content),
            Status = product.Status,
            IsPublished = product.IsPublished,
            DisplayOrder = product.DisplayOrder,
            CreatedBy = product.CreatedBy,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Translations = product.Translations is null
                ? null
                : new Response.ProductTranslationsResponse
                {
                    En = product.Translations.En is null
                        ? null
                        : new Response.ProductEnglishTranslationResponse
                        {
                            Content = ToDetailContentResponse(product.Translations.En.Content)
                        }
                }
        };
    }

    private static Response.ProductContentResponse? ToDetailContentResponse(ProductContent? content)
    {
        if (content is null)
        {
            return null;
        }

        return new Response.ProductContentResponse
        {
            Blocks = content.Blocks
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

    private sealed record UploadedProductImage(string Url, string PublicId);
}
