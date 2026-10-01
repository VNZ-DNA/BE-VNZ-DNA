namespace VNZ.Service.ProductService;

public interface IService
{
    Task<Response.ProductDetailResponse> CreateProductAsync(Request.CreateProductRequest request, Guid createdBy);
    Task<Response.ProductDetailResponse> GetProductDetailAsync(Guid id);
    Task<Response.ProductDetailResponse> UpdateProductAsync(Guid id, Request.UpdateProductRequest request);
    Task<Response.DeleteProductResponse> DeleteProductAsync(Guid id);
    Task<Response.PagedProductListResponse> GetProductListAsync(Request.GetProductListRequest request);
    Task<Response.PublicProductListResponse> GetPublicProductListAsync();
    Task<List<Response.ProductOrderItemResponse>> GetOrderableProductsAsync();
    Task<List<Response.ProductOrderItemResponse>> ReorderProductsAsync(Request.ReorderProductsRequest request);
}
