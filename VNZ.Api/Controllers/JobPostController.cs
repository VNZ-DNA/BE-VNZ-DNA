using System.Security.Claims;
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteJobPost(Guid id)
    {
        var data = await _jobPostService.DeleteJobPostAsync(id);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Xóa tin tuyển dụng thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<IActionResult> CreateJobPost(
        [FromBody] Request.CreateJobPostRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var createdBy))
        {
            return Unauthorized();
        }

        var data = await _jobPostService.CreateJobPostAsync(request, createdBy);
        var response = ResponseBuilder.SuccessResponse(
            data,
            request.Action == JobPostAction.SavedDraft
                ? "Lưu tin tuyển dụng thành công."
                : "Đăng tin tuyển dụng thành công.",
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status201Created, response);
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJobPostDetail(Guid id)
    {
        var data = await _jobPostService.GetJobPostDetailAsync(id);

        var response = ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết tin tuyển dụng thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateJobPost(
        Guid id,
        [FromBody] Request.UpdateJobPostRequest request)
    {
        var data = await _jobPostService.UpdateJobPostAsync(id, request);

        var response = ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật tin tuyển dụng thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
