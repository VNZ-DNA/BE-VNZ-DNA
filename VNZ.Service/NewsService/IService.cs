namespace VNZ.Service.NewsService;

public interface IService
{
    Task<Response.CreateNewsResponse> CreateNewsAsync(
        Request.CreateNewsRequest request,
        Guid createdBy);

    Task<Response.UpdateNewsResponse> UpdateNewsAsync(
        Guid id,
        Request.UpdateNewsRequest request);

    Task<Response.PagedNewsListResponse> GetNewsListAsync(
        Request.GetNewsListRequest request);

    Task<Response.PagedPublicNewsListResponse> GetPublicNewsListAsync(
        Request.GetPublicNewsListRequest request);

    Task<Response.NewsDetailResponse> GetNewsDetailAsync(Guid id);

    Task<List<Response.NewsCategoryResponse>> GetNewsCategoriesAsync();
}
