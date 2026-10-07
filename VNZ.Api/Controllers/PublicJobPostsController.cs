
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.JobPostService;
using VNZ.Service.Models;
using VNZ.Service.Localization;

namespace VNZ.Api.Controllers;

[ApiController]

[AllowAnonymous]

[Route("api/v1/public/job-posts")]
public sealed class PublicJobPostsController : ControllerBase
{
    private readonly IService _jobPostService;

    public PublicJobPostsController(IService jobPostService)
    {
        _jobPostService = jobPostService;
    }


    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublicJobPostDetail(Guid id)
    {
        var lang = LocaleResolver.Resolve(Request.Query["lang"].ToArray());
        var data = await _jobPostService.GetPublicJobPostDetailAsync(id, lang);

        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy chi tiết vị trí tuyển dụng thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicJobPostList()
    {
        var lang = LocaleResolver.Resolve(Request.Query["lang"].ToArray());
        var data = await _jobPostService.GetPublicJobPostListAsync(lang);

        return Ok(ResponseBuilder.SuccessResponse(data, "Lấy danh sách vị trí tuyển dụng thành công.", HttpContext.TraceIdentifier));
    }
}
