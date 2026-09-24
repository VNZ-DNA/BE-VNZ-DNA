namespace VNZ.Service.ProductService;

public class Request
{
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
