namespace VNZ.Service.JobPostService;

public interface IService
{
    Task<Response.CreateJobPostResponse> CreateJobPostAsync(
        Request.CreateJobPostRequest request,
        Guid createdBy);

    Task<Response.PagedJobPostListResponse> GetJobPostListAsync(
        Request.GetJobPostListRequest request);

    Task<Response.JobPostDetailResponse> GetJobPostDetailAsync(Guid id);
}
