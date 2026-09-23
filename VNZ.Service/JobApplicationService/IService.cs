namespace VNZ.Service.JobApplicationService;

public interface IService
{
    Task<Response.ReviewJobApplicationResponse> ReviewAsync(
        Guid id,
        Request.ReviewJobApplicationRequest request,
        Guid adminUserId);
}
