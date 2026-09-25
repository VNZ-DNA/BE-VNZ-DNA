namespace VNZ.Service.TeamMembers;

public interface IService
{
    Task<Response.PagedTeamMemberListResponse> GetMemberListAsync(Request.GetTeamMemberListRequest request);
    Task<List<Response.OrderableTeamMemberResponse>> GetOrderableMembersAsync();
    Task<List<Response.OrderableTeamMemberResponse>> ReorderMembersAsync(Request.ReorderTeamMembersRequest request);
    Task<Response.TeamMemberResponse> GetMemberByIdAsync(Guid id);
    Task<Response.TeamMemberResponse> CreateMemberAsync(Request.CreateTeamMemberRequest request, Guid createdBy);
    Task<Response.TeamMemberResponse> UpdateMemberAsync(Guid id, Request.UpdateTeamMemberRequest request);
}
