namespace VNZ.Service.JobApplicationService;

public interface IService
{
    Task<Response.CreateJobApplicationResponse> CreateAsync(Request.CreateJobApplicationRequest request);
    Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(Request.GetJobApplicationListRequest request);
    Task<Response.JobApplicationDetailResponse> GetJobApplicationByIdAsync(Guid id);
    Task<Response.ReviewJobApplicationResponse> ReviewAsync(Guid id, Request.ReviewJobApplicationRequest request, Guid adminUserId);
}
