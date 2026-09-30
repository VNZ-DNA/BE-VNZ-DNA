namespace VNZ.Service.JobApplicationService;

public interface IService
{
    Task<Response.CreateJobApplicationResponse> CreateAsync(Request.CreateJobApplicationRequest request);
    Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(Request.GetJobApplicationListRequest request);
    Task<Response.JobApplicationDetailResponse> GetJobApplicationByIdAsync(Guid id);

    Task<Response.ReviewJobApplicationResponse> ReviewAsync(
        Guid id,
        Request.ReviewJobApplicationRequest request,
        Guid adminUserId);
    Task<Response.SendInterviewInvitationsResponse> SendInterviewInvitationsAsync(
        Request.SendInterviewInvitationsRequest request);
    Task<Response.InterviewInvitationPreviewResponse> PreviewInterviewInvitationAsync(
        Request.SendInterviewInvitationsRequest request);

}
