namespace VNZ.Service.ContactService;

public interface IService
{
    Task<Response.ContactListResponse> GetContactListAsync(
        Request.GetContactListRequest request);

    Task<Response.ContactDetailResponse> GetContactDetailAsync(Guid id);
}
