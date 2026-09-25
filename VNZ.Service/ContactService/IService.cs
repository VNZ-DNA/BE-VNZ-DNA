namespace VNZ.Service.ContactService;

public interface IService
{
    Task<Response.ContactListResponse> GetContactListAsync(
        Request.GetContactListRequest request);

    Task<Response.ContactDetailResponse> GetContactDetailAsync(Guid id);

    Task<Response.SendContactReplyResponse> SendReplyAsync(
        Guid id,
        Request.SendContactReplyRequest request);
}
