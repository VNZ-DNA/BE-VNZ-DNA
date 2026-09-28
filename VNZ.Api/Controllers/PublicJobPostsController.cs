using Microsoft.AspNetCore.Mvc;
using VNZ.Service.JobPostService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/job-posts")]
public sealed class PublicJobPostsController : ControllerBase
{
    private readonly IService _jobPostService;

    public PublicJobPostsController(IService jobPostService)
    {
        _jobPostService = jobPostService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicJobPostList()
    {
        var data = await _jobPostService.GetPublicJobPostListAsync();

        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách vị trí tuyển dụng thành công.", HttpContext.TraceIdentifier));
    }
}
