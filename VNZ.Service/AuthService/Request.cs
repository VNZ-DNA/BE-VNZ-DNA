namespace VNZ.Service.AuthService;

public class Request
{
    public class LoginRequest
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
    }

    public class RefreshRequest
    {
        public string? RefreshToken { get; init; }
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; init; }
    }

    public class RegisterRequest
    {
        public required string FullName { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public required string Password { get; set; } = string.Empty;
    }
    
    public class ForgotPasswordRequest
    {
        public required string Email { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public int Code { get; set; }
        public required string NewPassword { get; set; } = string.Empty;
    }
}
