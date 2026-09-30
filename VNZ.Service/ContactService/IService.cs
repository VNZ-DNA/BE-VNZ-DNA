namespace VNZ.Service.ContactService;

public interface IService
{
    Task<Response.ContactListResponse> GetContactListAsync(
        Request.GetContactListRequest request);

    Task<Response.ContactDetailResponse> GetContactDetailAsync(Guid id);

    Task<Response.SendContactReplyResponse> SendReplyAsync(
        Guid id,
        Request.SendContactReplyRequest request);

    Task<Response.ContactReplyPreviewResponse> PreviewReplyAsync(
        Guid id,
        Request.SendContactReplyRequest request);

    Task<Response.CreateContactInquiryResponse> CreateContactInquiryAsync(
        Request.CreateContactInquiryRequest request);
}
