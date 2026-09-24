namespace VNZ.Service.NewsService;

public interface IService
{
    Task<Response.PagedNewsListResponse> GetNewsListAsync(
        Request.GetNewsListRequest request);

    Task<List<Response.NewsCategoryResponse>> GetNewsCategoriesAsync();
}
