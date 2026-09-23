using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.DashboardService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IService _dashboardService;

    public DashboardController(IService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var data = await _dashboardService.GetDashboardAsync();
        var response = ResponseBuilder.SuccessResponse(
            data,
            "Lấy dữ liệu tổng quan thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
