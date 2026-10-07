namespace VNZ.Service.JobPostService;

public interface IService
{
    Task<Response.CreateJobPostResponse> CreateJobPostAsync(
        Request.CreateJobPostRequest request,
        Guid createdBy);

    Task<Response.PagedJobPostListResponse> GetJobPostListAsync(
        Request.GetJobPostListRequest request);

    Task<List<Response.PublicJobPostListItemResponse>> GetPublicJobPostListAsync(string? lang = null);

    Task<Response.JobPostDetailResponse> GetJobPostDetailAsync(Guid id);

    Task<Response.PublicJobPostDetailResponse> GetPublicJobPostDetailAsync(string slug, string? lang = null);

    Task<Response.UpdateJobPostResponse> UpdateJobPostAsync(
        Guid id,
        Request.UpdateJobPostRequest request);

    Task<Response.DeleteJobPostResponse> DeleteJobPostAsync(Guid id);
}
