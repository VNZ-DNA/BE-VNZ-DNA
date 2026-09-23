namespace VNZ.Service.AuthService;

public class Response
{
    public class RegisterResponse
    {
        public Guid Id { get; init; }
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public required string Role { get; init; }
    }

    public class LoginResponse
    {
        public required string AccessToken { get; set; }
        public required string TokenType { get; set; }
        public required DateTimeOffset ExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public required DateTimeOffset RefreshTokenExpiresAt { get; set; }
        public required string FullName { get; set; }
    }

    public class RefreshResponse
    {
        public required string AccessToken { get; set; }
        public required string TokenType { get; set; }
        public required DateTimeOffset ExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public required DateTimeOffset RefreshTokenExpiresAt { get; set; }
    }
}
