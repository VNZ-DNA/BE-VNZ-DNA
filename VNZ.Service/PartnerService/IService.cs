namespace VNZ.Service.PartnerService;

public interface IService
{
    Task<Response.PartnerListItemResponse> CreatePartnerAsync(Request.CreatePartnerRequest request, Guid createdBy);
    Task<Response.PartnerListItemResponse> GetPartnerDetailAsync(Guid id);
    Task<Response.PagedPartnerListResponse> GetPartnerListAsync(Request.GetPartnerListRequest request);
    Task<Response.PublicPartnerListResponse> GetPublicPartnerListAsync();
    Task<List<Response.PartnerListItemResponse>> GetOrderablePartnersAsync();
    Task<List<Response.PartnerListItemResponse>> ReorderPartnersAsync(Request.ReorderPartnersRequest request);
    Task<Response.PartnerListItemResponse> UpdatePartnerAsync(Guid id, Request.UpdatePartnerRequest request);
    Task<Response.DeletePartnerResponse> DeletePartnerAsync(Guid id);
}
