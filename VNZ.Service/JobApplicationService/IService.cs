namespace VNZ.Service.JobApplicationService;

public interface IService
{
    Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(
        Request.GetJobApplicationListRequest request);

    Task<Response.ReviewJobApplicationResponse> ReviewAsync(
        Guid id,
        Request.ReviewJobApplicationRequest request,
        Guid adminUserId);
}
