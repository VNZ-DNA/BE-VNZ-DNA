using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.DepartmentService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/departments")]
[Authorize(Roles = "Admin")]
public class DepartmentController : ControllerBase
{
    private readonly IService _departmentService;

    public DepartmentController(IService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments()
    {
        var data = await _departmentService.GetDepartmentsAsync();

        var response = ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách phòng ban thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
