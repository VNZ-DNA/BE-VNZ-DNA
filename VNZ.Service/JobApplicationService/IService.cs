namespace VNZ.Service.JobApplicationService;

public interface IService
{
    Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(Request.GetJobApplicationListRequest request);
    Task<Response.JobApplicationDetailResponse> GetJobApplicationByIdAsync(Guid id);
}
