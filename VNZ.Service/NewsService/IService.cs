namespace VNZ.Service.NewsService;

public interface IService
{
    Task<Response.PagedNewsListResponse> GetNewsListAsync(
        Request.GetNewsListRequest request);

    Task<Response.NewsDetailResponse> GetNewsDetailAsync(Guid id);

    Task<List<Response.NewsCategoryResponse>> GetNewsCategoriesAsync();
}
