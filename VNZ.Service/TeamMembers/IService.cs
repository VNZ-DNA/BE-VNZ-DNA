namespace VNZ.Service.TeamMembers;

public interface IService
{
    Task<Response.TeamMemberResponse> GetMemberByIdAsync(Guid id);
    Task<Response.TeamMemberResponse> CreateMemberAsync(Request.CreateTeamMemberRequest request, Guid createdBy);
    Task<Response.TeamMemberResponse> UpdateMemberAsync(Guid id, Request.UpdateTeamMemberRequest request);
}
