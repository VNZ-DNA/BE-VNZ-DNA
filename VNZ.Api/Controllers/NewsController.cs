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
