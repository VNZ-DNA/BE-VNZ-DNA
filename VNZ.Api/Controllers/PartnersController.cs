using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using PartnerService = VNZ.Service.PartnerService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/partners")]
[Authorize(Roles = "Admin")]
public sealed class PartnersController : ControllerBase
{
    private readonly PartnerService.IService _partnerService;

    public PartnersController(PartnerService.IService partnerService)
    {
        _partnerService = partnerService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePartner(
        [FromBody] PartnerService.Request.CreatePartnerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var createdBy))
        {
            return Unauthorized();
        }

        var data = await _partnerService.CreatePartnerAsync(request, createdBy);

        return StatusCode(StatusCodes.Status201Created, ResponseBuilder.SuccessResponse(
            data,
            "Tạo Partner thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<IActionResult> GetPartnerList(
        [FromQuery] PartnerService.Request.GetPartnerListRequest request)
    {
        var data = await _partnerService.GetPartnerListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách Partner thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPartnerDetail(Guid id)
    {
        var data = await _partnerService.GetPartnerDetailAsync(id);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết Partner thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdatePartner(
        Guid id,
        [FromBody] PartnerService.Request.UpdatePartnerRequest request)
    {
        var data = await _partnerService.UpdatePartnerAsync(id, request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật Partner thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpPut("display-order")]
    public async Task<IActionResult> ReorderPartners(
        [FromBody] PartnerService.Request.ReorderPartnersRequest request)
    {
        var data = await _partnerService.ReorderPartnersAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật thứ tự Partner thành công.",
            HttpContext.TraceIdentifier));
    }
}
