using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using TeamMemberService = VNZ.Service.TeamMembers;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/team-members")]
[Authorize(Roles = "Admin")]
public sealed class TeamMembersController : ControllerBase
{
    private readonly TeamMemberService.IService _teamMemberService;

    public TeamMembersController(TeamMemberService.IService teamMemberService)
    {
        _teamMemberService = teamMemberService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMemberList(
        [FromQuery] TeamMemberService.Request.GetTeamMemberListRequest request)
    {
        var data = await _teamMemberService.GetMemberListAsync(request);
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách thành viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpGet("display-order")]
    public async Task<IActionResult> GetOrderableMembers()
    {
        var data = await _teamMemberService.GetOrderableMembersAsync();
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách thứ tự thành viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpPut("display-order")]
    public async Task<IActionResult> ReorderMembers([FromBody] TeamMemberService.Request.ReorderTeamMembersRequest request)
    {
        var data = await _teamMemberService.ReorderMembersAsync(request);
        return Ok(ResponseBuilder.SuccessResponse(data, "Cập nhật thứ tự thành viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMemberById(Guid id)
    {
        var data = await _teamMemberService.GetMemberByIdAsync(id);
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy thông tin thành viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateMember([FromBody] TeamMemberService.Request.CreateTeamMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var createdBy))
        {
            return Unauthorized();
        }

        var data = await _teamMemberService.CreateMemberAsync(request, createdBy);
        var response = ResponseBuilder.SuccessResponse(data, "Tạo thành viên thành công.", HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateMember(Guid id, [FromBody] TeamMemberService.Request.UpdateTeamMemberRequest request)
    {
        var data = await _teamMemberService.UpdateMemberAsync(id, request);
        return Ok(ResponseBuilder.SuccessResponse(data, "Cập nhật thành viên thành công.", HttpContext.TraceIdentifier));
    }
}
