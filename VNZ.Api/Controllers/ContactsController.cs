using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using ContactService = VNZ.Service.ContactService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/contacts")]
[Authorize(Roles = "Admin")]
public class ContactsController : ControllerBase
{
    private readonly ContactService.IService _contactService;

    public ContactsController(ContactService.IService contactService)
    {
        _contactService = contactService;
    }

    [HttpGet]
    public async Task<IActionResult> GetContactList(
        [FromQuery] ContactService.Request.GetContactListRequest request)
    {
        var data = await _contactService.GetContactListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách liên hệ thành công.",
            HttpContext.TraceIdentifier));
    }
}
