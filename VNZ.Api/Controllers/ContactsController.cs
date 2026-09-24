using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Exceptions;
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContactDetail(string id)
    {
        if (!Guid.TryParse(id, out var contactId))
        {
            throw new ContactException(
                "CONTACT_ID_INVALID",
                "Mã liên hệ không hợp lệ.",
                "id");
        }

        var data = await _contactService.GetContactDetailAsync(contactId);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết liên hệ thành công.",
            HttpContext.TraceIdentifier));
    }
}
