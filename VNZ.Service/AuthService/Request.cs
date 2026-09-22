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
}
