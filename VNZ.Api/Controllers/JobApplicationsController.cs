using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using JobApplicationService = VNZ.Service.JobApplicationService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/job-applications")]
[Authorize(Roles = "Admin")]
public sealed class JobApplicationsController : ControllerBase
{
    private readonly JobApplicationService.IService _jobApplicationService;

    public JobApplicationsController(JobApplicationService.IService jobApplicationService)
    {
        _jobApplicationService = jobApplicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobApplicationList(
        [FromQuery] JobApplicationService.Request.GetJobApplicationListRequest request)
    {
        var data = await _jobApplicationService.GetJobApplicationListAsync(request);
        var response = ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách hồ sơ ứng viên thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] JobApplicationService.Request.ReviewJobApplicationRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var adminUserId))
        {
            return Unauthorized();
        }

        var data = await _jobApplicationService.ReviewAsync(id, request, adminUserId);
        var response = ResponseBuilder.SuccessResponse(data, "Review hồ sơ ứng tuyển thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
