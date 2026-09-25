namespace VNZ.Service.ProductService;

public interface IService
{
    Task<Response.ProductDetailResponse> CreateProductAsync(Request.CreateProductRequest request, Guid createdBy);
    Task<Response.ProductDetailResponse> GetProductDetailAsync(Guid id);
    Task<Response.ProductDetailResponse> UpdateProductAsync(Guid id, Request.UpdateProductRequest request);
    Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request);
    Task<List<Response.OrderableProductResponse>> GetOrderableProductsAsync();
    Task<List<Response.ProductListItemResponse>> ReorderProductsAsync(Request.ReorderProductsRequest request);
}
