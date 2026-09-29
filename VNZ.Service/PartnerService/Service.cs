using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Service.Exceptions;
using MediaService = VNZ.Service.Utils.MediaService;

namespace VNZ.Service.PartnerService;

public sealed class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly MediaService.IService _mediaService;

    public Service(
        AppDbContext dbContext,
        MediaService.IService mediaService)
    {
        _dbContext = dbContext;
        _mediaService = mediaService;
    }

    public async Task<Response.PartnerListItemResponse> CreatePartnerAsync(Request.CreatePartnerRequest request, Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new PartnerException(
                "PARTNER_VALIDATION_FAILED",
                "Tên Partner là bắt buộc.",
                "name");
        }

        var logoUrl = request.LogoUrl;

        if (request.Logo is not null)
        {
            var uploadResult = await _mediaService.UploadImageAsync(
                new MediaService.Request.UploadImageRequest
                {
                    File = request.Logo,
                    Purpose = "PartnerLogo"
                });

            logoUrl = uploadResult.Url;
        }

        var now = DateTimeOffset.UtcNow;
        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            Name = name,
            LogoUrl = logoUrl,
            WebsiteUrl = request.WebsiteUrl,
            Description = request.Description,
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = now,
            UpdateAt = now
        };

        try
        {
            _dbContext.Partners.Add(partner);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new PartnerException(
                "PARTNER_CREATE_FAILED",
                "Không thể tạo Partner.",
                exception);
        }

        return ToPartnerResponse(partner);
    }

    public async Task<Response.PartnerListItemResponse> GetPartnerDetailAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new PartnerException(
                "PARTNER_VALIDATION_FAILED",
                "ID Partner không hợp lệ.",
                "id");
        }

        var partner = await _dbContext.Partners
            .AsNoTracking()
            .FirstOrDefaultAsync(partner => partner.Id == id);

        if (partner is null)
        {
            throw new PartnerException(
                "PARTNER_NOT_FOUND",
                "Không tìm thấy Partner.");
        }

        return ToPartnerResponse(partner);
    }

    public async Task<Response.PagedPartnerListResponse> GetPartnerListAsync(
        Request.GetPartnerListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
        {
            throw new PartnerException("PARTNER_LIST_VALIDATION_FAILED", "Thông tin phân trang không hợp lệ.", "page", "pageSize");
        }

        var search = request.Search?.Trim();
        var query = _dbContext.Partners
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(partner => partner.Name.ToLower().Contains(searchLower));
        }

        try
        {
            var total = await query.CountAsync();

            var partners = await query
                .OrderByDescending(partner => partner.IsPublished)
                .ThenBy(partner => partner.DisplayOrder)
                .ThenByDescending(partner => partner.CreatedAt)
                .ThenByDescending(partner => partner.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = partners.Select(ToPartnerResponse).ToList();

            return new Response.PagedPartnerListResponse
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                Total = total,
                TotalPages = total == 0
                    ? 0
                    : (int)Math.Ceiling(total / (double)request.PageSize)
            };
        }
        catch (DbException exception)
        {
            throw new PartnerException("PARTNER_LIST_READ_FAILED", "Không thể đọc danh sách Partner.", exception);
        }
    }

    public async Task<Response.PublicPartnerListResponse> GetPublicPartnerListAsync()
    {
        var query = _dbContext.Partners
            .AsNoTracking()
            .Where(partner => partner.IsPublished && partner.DisplayOrder != null);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(partner => partner.DisplayOrder)
            .ThenBy(partner => partner.Id)
            .Select(partner => new Response.PublicPartnerListItemResponse
            {
                LogoUrl = partner.LogoUrl,
                WebsiteUrl = partner.WebsiteUrl
            })
            .ToListAsync();

        return new Response.PublicPartnerListResponse
        {
            Total = total,
            Items = items
        };
    }

    public async Task<List<Response.PartnerListItemResponse>> GetOrderablePartnersAsync()
    {
        try
        {
            var partners = await _dbContext.Partners
                .AsNoTracking()
                .Where(partner => partner.IsPublished)
                .OrderBy(partner => partner.DisplayOrder)
                .ToListAsync();

            return partners.Select(ToPartnerResponse).ToList();
        }
        catch (Exception exception) when (exception is not PartnerException &&
                                          exception is not OperationCanceledException)
        {
            throw new PartnerException(
                "PARTNER_LIST_READ_FAILED",
                "Không thể đọc danh sách Partner để sắp xếp.",
                exception);
        }
    }

    public async Task<List<Response.PartnerListItemResponse>> ReorderPartnersAsync(
        Request.ReorderPartnersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderedPartnerIds = request.OrderedPartnerIds;

        if (orderedPartnerIds is null || orderedPartnerIds.Count == 0 ||
            orderedPartnerIds.Distinct().Count() != orderedPartnerIds.Count)
        {
            throw new PartnerException(
                "PARTNER_ORDER_INVALID",
                "Danh sách sắp xếp Partner không hợp lệ.",
                "orderedPartnerIds");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var publishedPartners = await _dbContext.Partners
                .Where(partner => partner.IsPublished)
                .OrderBy(partner => partner.DisplayOrder)
                .ThenBy(partner => partner.CreatedAt)
                .ThenBy(partner => partner.Id)
                .ToListAsync();

            var publishedPartnerIds = publishedPartners
                .Select(partner => partner.Id)
                .ToHashSet();

            if (publishedPartnerIds.Count != orderedPartnerIds.Count ||
                orderedPartnerIds.Any(id => !publishedPartnerIds.Contains(id)))
            {
                throw new PartnerException(
                    "PARTNER_ORDER_INVALID",
                    "Danh sách sắp xếp Partner không hợp lệ.",
                    "orderedPartnerIds");
            }

            var partnersById = publishedPartners.ToDictionary(partner => partner.Id);
            var updatedAt = DateTimeOffset.UtcNow;

            for (var index = 0; index < orderedPartnerIds.Count; index++)
            {
                var partner = partnersById[orderedPartnerIds[index]];
                partner.DisplayOrder = index + 1;
                partner.UpdateAt = updatedAt;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return orderedPartnerIds
                .Select(id => ToPartnerResponse(partnersById[id]))
                .ToList();
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new PartnerException(
                "PARTNER_ORDER_UPDATE_FAILED",
                "Không thể cập nhật thứ tự Partner.",
                exception);
        }
    }

    public async Task<Response.PartnerListItemResponse> UpdatePartnerAsync(
        Guid id,
        Request.UpdatePartnerRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            throw new PartnerException(
                "PARTNER_VALIDATION_FAILED",
                "ID Partner không hợp lệ.",
                "id");
        }

        var name = request.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new PartnerException(
                "PARTNER_VALIDATION_FAILED",
                "Tên Partner là bắt buộc.",
                "name");
        }

        if (!request.IsPublished.HasValue)
        {
            throw new PartnerException(
                "PARTNER_VALIDATION_FAILED",
                "Trạng thái đăng Partner là bắt buộc.",
                "isPublished");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);

            var partner = await _dbContext.Partners
                .FirstOrDefaultAsync(partner => partner.Id == id);

            if (partner is null)
            {
                throw new PartnerException(
                    "PARTNER_NOT_FOUND",
                    "Không tìm thấy Partner.");
            }

            var isLogoChanged = request.Logo is not null ||
                (request.LogoUrl is not null && partner.LogoUrl != request.LogoUrl);

            var isInformationChanged = partner.Name != name ||
                isLogoChanged ||
                partner.WebsiteUrl != request.WebsiteUrl ||
                partner.Description != request.Description;

            if (partner.IsPublished && isInformationChanged)
            {
                throw new PartnerException(
                    "PARTNER_PUBLISHED_CANNOT_EDIT",
                    "Partner đang Đã đăng phải được gỡ đăng trước khi chỉnh sửa.",
                    "name",
                    "logoUrl",
                    "websiteUrl",
                    "description");
            }

            var updatedAt = DateTimeOffset.UtcNow;

            if (partner.IsPublished && !request.IsPublished.Value)
            {
                partner.IsPublished = false;
                partner.DisplayOrder = null;
                partner.UpdateAt = updatedAt;

                var publishedPartners = await _dbContext.Partners
                    .Where(item => item.IsPublished && item.Id != partner.Id)
                    .OrderBy(item => item.DisplayOrder)
                    .ThenBy(item => item.CreatedAt)
                    .ThenBy(item => item.Id)
                    .ToListAsync();

                for (var index = 0; index < publishedPartners.Count; index++)
                {
                    var publishedPartner = publishedPartners[index];
                    publishedPartner.DisplayOrder = index + 1;
                    publishedPartner.UpdateAt = updatedAt;
                }
            }
            else if (!partner.IsPublished)
            {
                var logoUrl = partner.LogoUrl;

                if (request.Logo is not null)
                {
                    var uploadResult = await _mediaService.UploadImageAsync(
                        new MediaService.Request.UploadImageRequest
                        {
                            File = request.Logo,
                            Purpose = "PartnerLogo"
                        });

                    logoUrl = uploadResult.Url;
                }
                else if (request.LogoUrl is not null)
                {
                    logoUrl = request.LogoUrl;
                }

                partner.Name = name;
                partner.LogoUrl = logoUrl;
                partner.WebsiteUrl = request.WebsiteUrl;
                partner.Description = request.Description;
                partner.IsPublished = request.IsPublished.Value;
                partner.UpdateAt = updatedAt;

                if (partner.IsPublished)
                {
                    var lastDisplayOrder = await _dbContext.Partners
                        .Where(item => item.IsPublished)
                        .MaxAsync(item => (int?)item.DisplayOrder) ?? 0;

                    partner.DisplayOrder = lastDisplayOrder + 1;
                }
                else
                {
                    partner.DisplayOrder = null;
                }
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToPartnerResponse(partner);
        }
        catch (Exception exception) when (exception is DbUpdateException or DbException)
        {
            throw new PartnerException(
                "PARTNER_UPDATE_FAILED",
                "Không thể cập nhật Partner.",
                exception);
        }
    }

    private static Response.PartnerListItemResponse ToPartnerResponse(Partner partner)
    {
        return new Response.PartnerListItemResponse
        {
            Id = partner.Id,
            Name = partner.Name,
            LogoUrl = partner.LogoUrl,
            WebsiteUrl = partner.WebsiteUrl,
            Description = partner.Description,
            IsPublished = partner.IsPublished,
            DisplayOrder = partner.DisplayOrder,
            CreatedBy = partner.CreatedBy,
            CreatedAt = partner.CreatedAt,
            UpdatedAt = partner.UpdateAt
        };
    }
}
