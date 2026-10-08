using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VNZ.Api.Filters;
using VNZ.Api.RateLimiting;
using VNZ.Service.Models;
using ContactService = VNZ.Service.ContactService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/contacts")]
[AllowAnonymous]
[ServiceFilter(typeof(PublicContactApiResultFilter))]
public class PublicContactsController : ControllerBase
{
    private readonly ContactService.IService _contactService;

    public PublicContactsController(ContactService.IService contactService)
    {
        _contactService = contactService;
    }

    [HttpPost]
    [Consumes("application/json")]
    [EnableRateLimiting(RateLimitPolicies.ContactInquiryCreate)]
    public async Task<IActionResult> CreateContactInquiry(
        [FromBody] ContactService.Request.CreateContactInquiryRequest request)
    {
        var data = await _contactService.CreateContactInquiryAsync(request);

        return StatusCode(StatusCodes.Status201Created, ResponseBuilder.SuccessResponse(
            data,
            "Gửi yêu cầu liên hệ thành công.",
            HttpContext.TraceIdentifier));
    }
}
