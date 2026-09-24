using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
