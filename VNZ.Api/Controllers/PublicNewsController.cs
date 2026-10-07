using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using VNZ.Service.Localization;
using NewsService = VNZ.Service.NewsService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/news")]
[AllowAnonymous]
public sealed class PublicNewsController : ControllerBase
{
    private readonly NewsService.IService _newsService;

    public PublicNewsController(NewsService.IService newsService)
    {
        _newsService = newsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicNewsList(
        [FromQuery] NewsService.Request.GetPublicNewsListRequest request)
    {
        request.Lang = LocaleResolver.Resolve(Request.Query["lang"].ToArray());
        var data = await _newsService.GetPublicNewsListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách tin tức thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetPublicNewsDetail(string slug)
    {
        var lang = LocaleResolver.Resolve(Request.Query["lang"].ToArray());
        var data = await _newsService.GetPublicNewsDetailAsync(slug, lang);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết tin tức thành công.",
            HttpContext.TraceIdentifier));
    }
}
