namespace VNZ.Service.ProductService;

public interface IService
{
    Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request);
    Task<List<Response.ProductListItemResponse>> ReorderProductsAsync(Request.ReorderProductsRequest request);
}
