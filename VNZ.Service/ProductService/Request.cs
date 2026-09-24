using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.ProductService;

public class Request
{
    public class UpdateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? ProductUrl { get; set; }
        public ProductContentRequest? Content { get; set; }
        public ProductStatus Status { get; set; }
        public bool IsPublished { get; set; }
    }

    public class ProductContentRequest
    {
        public List<ContentBlockRequest> Blocks { get; set; } = new();
    }

    public class ContentBlockRequest
    {
        public Guid Id { get; set; }
        public ContentBlockType Type { get; set; }
        public int Order { get; set; }
        public string? Text { get; set; }
        public List<FeatureItemRequest>? Items { get; set; }
    }

    public class FeatureItemRequest
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
    }

    public class GetProductListRequest
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ReorderProductsRequest
    {
        public List<Guid>? OrderedProductIds { get; set; }
    }
}
