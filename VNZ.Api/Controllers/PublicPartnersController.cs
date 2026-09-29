using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using PartnerService = VNZ.Service.PartnerService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/partners")]
[AllowAnonymous]
public sealed class PublicPartnersController : ControllerBase
{
    private readonly PartnerService.IService _partnerService;

    public PublicPartnersController(PartnerService.IService partnerService)
    {
        _partnerService = partnerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicPartnerList()
    {
        var data = await _partnerService.GetPublicPartnerListAsync();

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách Partner công khai thành công.",
            HttpContext.TraceIdentifier));
    }
}
