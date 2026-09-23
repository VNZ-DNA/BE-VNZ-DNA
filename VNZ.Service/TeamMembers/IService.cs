namespace VNZ.Service.TeamMembers;

public interface IService
{
    Task<List<Response.TeamMemberResponse>> GetMemberListAsync();
    Task<List<Response.TeamMemberResponse>> ReorderMembersAsync(Request.ReorderTeamMembersRequest request);
    Task<Response.TeamMemberResponse> GetMemberByIdAsync(Guid id);
    Task<Response.TeamMemberResponse> CreateMemberAsync(Request.CreateTeamMemberRequest request, Guid createdBy);
    Task<Response.TeamMemberResponse> UpdateMemberAsync(Guid id, Request.UpdateTeamMemberRequest request);
}
