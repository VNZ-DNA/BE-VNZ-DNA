namespace VNZ.Service.AuthService;

public interface IService
{
    Task<Response.LoginResponse> LoginAsync(Request.LoginRequest request);
    Task<Response.RefreshResponse> RefreshAsync(Request.RefreshRequest request);
    Task LogoutAsync(Request.LogoutRequest request);
    Task<Response.RegisterResponse> Register(Request.RegisterRequest request);
    Task<string> ForgotPassword(Request.ForgotPasswordRequest request);
    Task<string> ChangePassword(Request.ChangePasswordRequest request);
}
