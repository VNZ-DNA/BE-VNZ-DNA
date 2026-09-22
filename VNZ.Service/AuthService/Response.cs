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
        public required string AccessToken { get; init; }
        public required string TokenType { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public required string RefreshToken { get; init; }
        public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
        public required string FullName { get; init; }
    }

    public class RefreshResponse
    {
        public required string AccessToken { get; init; }
        public required string TokenType { get; init; }
        public required DateTimeOffset ExpiresAt { get; init; }
        public required string RefreshToken { get; init; }
        public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
    }
}
