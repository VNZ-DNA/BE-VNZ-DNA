using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.JobPostService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/job-posts")]
[Authorize(Roles = "Admin")]
public class JobPostController : ControllerBase
{
    private readonly IService _jobPostService;

    public JobPostController(IService jobPostService)
    {
        _jobPostService = jobPostService;
    }

    [HttpGet]
    public async Task<IActionResult> GetJobPostList([FromQuery] Request.GetJobPostListRequest request)
    {
        var data = await _jobPostService.GetJobPostListAsync(request);

        var response = ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách tin tuyển dụng thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
