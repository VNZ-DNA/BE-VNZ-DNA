using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.JobApplicationService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/job-applications")]
[Authorize(Roles = "Admin")]
public sealed class JobApplicationController : ControllerBase
{
    private readonly IService _jobApplicationService;

    public JobApplicationController(IService jobApplicationService)
    {
        _jobApplicationService = jobApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobApplicationList([FromQuery] Request.GetJobApplicationListRequest request)
    {
        var data = await _jobApplicationService.GetJobApplicationListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách hồ sơ ứng viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobApplicationById(Guid id)
    {
        var data = await _jobApplicationService.GetJobApplicationByIdAsync(id);

        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy chi tiết hồ sơ ứng viên thành công.", HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] Request.ReviewJobApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var adminUserId))
        {
            return Unauthorized();
        }

        var data = await _jobApplicationService.ReviewAsync(id, request, adminUserId);
        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Review hồ sơ ứng viên thành công.",
            HttpContext.TraceIdentifier));
    }
}
