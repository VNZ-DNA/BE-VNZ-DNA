namespace VNZ.Service.AuthService;

public class Request
{
    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class RefreshRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class LogoutRequest
    {
        public string? RefreshToken { get; set; }
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
