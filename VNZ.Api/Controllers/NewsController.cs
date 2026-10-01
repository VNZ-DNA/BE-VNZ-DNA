using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Exceptions;
using VNZ.Service.Models;
using NewsService = VNZ.Service.NewsService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/news")]
[Authorize(Roles = "Admin")]
public sealed class NewsController : ControllerBase
{
    private readonly NewsService.IService _newsService;

    public NewsController(NewsService.IService newsService)
    {
        _newsService = newsService;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNews(Guid id)
    {
        var data = await _newsService.DeleteNewsAsync(id);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Xóa bài viết thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpPost("content-images")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadContentImage(
        [FromForm] NewsService.Request.UploadContentImageRequest request)
    {
        var data = await _newsService.UploadContentImageAsync(request);

        return StatusCode(
            StatusCodes.Status201Created,
            ResponseBuilder.SuccessResponse(
                data,
                "Tải ảnh nội dung thành công.",
                HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateNews([FromForm] NewsService.Request.CreateNewsRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var createdBy))
        {
            throw new UnauthorizedException("Yêu cầu đăng nhập để tiếp tục.");
        }

        var data = await _newsService.CreateNewsAsync(request, createdBy);
        var message = request.Status == "Draft"
            ? "Lưu bản nháp thành công."
            : "Đăng bài viết thành công.";

        return StatusCode(
            StatusCodes.Status201Created,
            ResponseBuilder.SuccessResponse(data, message, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateNews(
        string id,
        [FromForm] NewsService.Request.UpdateNewsRequest request)
    {
        var isValidId = Guid.TryParse(id, out var newsId);

        if (!isValidId)
        {
            throw new NewsException(
                "NEWS_ARTICLE_ID_INVALID",
                "Mã bài viết không hợp lệ.",
                "id");
        }

        var data = await _newsService.UpdateNewsAsync(newsId, request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật bài viết thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetNewsList(
        [FromQuery] NewsService.Request.GetNewsListRequest request)
    {
        var data = await _newsService.GetNewsListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách bài viết thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetNewsCategories()
    {
        var data = await _newsService.GetNewsCategoriesAsync();

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách danh mục bài viết thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNewsDetail(string id)
    {
        var isValidId = Guid.TryParse(id, out var newsId);

        if (!isValidId)
        {
            throw new NewsException(
                "NEWS_ARTICLE_ID_INVALID",
                "Mã bài viết không hợp lệ.",
                "id");
        }

        var data = await _newsService.GetNewsDetailAsync(newsId);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết bài viết thành công.",
            HttpContext.TraceIdentifier));
    }
}
