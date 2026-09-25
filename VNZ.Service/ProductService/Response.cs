using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.ProductService;

public class Response
{
    public class ProductDetailResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? ProductUrl { get; set; }
        public ProductContentResponse? Content { get; set; }
        public ProductStatus Status { get; set; }
        public bool IsPublished { get; set; }
        public int? DisplayOrder { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class ProductContentResponse
    {
        public List<ContentBlockResponse> Blocks { get; set; } = new();
    }

    public class ContentBlockResponse
    {
        public Guid Id { get; set; }
        public ContentBlockType Type { get; set; }
        public int Order { get; set; }
        public string? Text { get; set; }
        public List<FeatureItemResponse>? Items { get; set; }
    }

    public class FeatureItemResponse
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
    }

    public class PagedProductListResponse
    {
        public required List<ProductListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductListItemResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? LogoUrl { get; set; }
        public string? ProductUrl { get; set; }
        public string? Summary { get; set; }
        public ProductStatus Status { get; set; }
        public bool IsPublished { get; set; }
        public int? DisplayOrder { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
