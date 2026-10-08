using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VNZ.Api.RateLimiting;
using VNZ.Service.JobApplicationService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/job-applications")]
public sealed class PublicJobApplicationsController : ControllerBase
{
    private readonly IService _jobApplicationService;

    public PublicJobApplicationsController(IService jobApplicationService)
    {
        _jobApplicationService = jobApplicationService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.JobApplicationCreate)]
    public async Task<IActionResult> Create([FromBody] Request.CreateJobApplicationRequest request)
    {
        var data = await _jobApplicationService.CreateAsync(request);
        var response = ResponseBuilder.SuccessResponse(data, "Gửi hồ sơ ứng tuyển thành công.", HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}
