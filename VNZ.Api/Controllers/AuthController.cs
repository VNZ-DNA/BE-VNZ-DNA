using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.AuthService;
using VNZ.Service.Models;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IService _authService;

    public AuthController(IService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] Request.LoginRequest request)
    {
        var data = await _authService.LoginAsync(request);
        var response = ResponseBuilder.SuccessResponse(
            data,
            "Đăng nhập thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] Request.RefreshRequest request)
    {
        var data = await _authService.RefreshAsync(request);
        var response = ResponseBuilder.SuccessResponse(
            data,
            "Làm mới phiên đăng nhập thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] Request.LogoutRequest request)
    {
        await _authService.LogoutAsync(request);
        var response = ResponseBuilder.SuccessResponse(
            data: null,
            message: "Đăng xuất thành công.",
            traceId: HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Request.RegisterRequest request)
    {
        var data = await _authService.Register(request);
        var response = ResponseBuilder.SuccessResponse(
            data,
            "Tạo tài khoản thành công.",
            HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] Request.ForgotPasswordRequest request)
    {
        var message = await _authService.ForgotPassword(request);
        var response = ResponseBuilder.SuccessResponse(
            data: null,
            message: message,
            traceId: HttpContext.TraceIdentifier);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] Request.ChangePasswordRequest request)
    {
        var message = await _authService.ChangePassword(request);
        var response = ResponseBuilder.SuccessResponse(
            data: null,
            message: message,
            traceId: HttpContext.TraceIdentifier);

        return Ok(response);
    }
}
