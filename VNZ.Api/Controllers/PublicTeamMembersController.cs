using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using VNZ.Service.TeamMembers;

namespace VNZ.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/public/team-members")]
public sealed class PublicTeamMembersController : ControllerBase
{
    private readonly IService _teamMemberService;

    public PublicTeamMembersController(IService teamMemberService)
    {
        _teamMemberService = teamMemberService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicMemberList()
    {
        var data = await _teamMemberService.GetPublicMemberListAsync();
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách thành viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeaturedMembers()
    {
        var data = await _teamMemberService.GetFeaturedMembersAsync();
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách thành viên nổi bật thành công.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublicMemberById(Guid id)
    {
        var data = await _teamMemberService.GetPublicMemberByIdAsync(id);
        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy thông tin thành viên thành công.", HttpContext.TraceIdentifier));
    }
}
