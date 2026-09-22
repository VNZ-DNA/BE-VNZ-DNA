using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Service.Exceptions;
using VNZ.Service.JwtService;

namespace VNZ.Service.AuthService;

public class Service : IService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(3);

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public Service(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<Response.LoginResponse> LoginAsync(Request.LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = request.Email?.Trim().ToLowerInvariant();
        var password = request.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
        {
            var fields = new List<string>();
            if (string.IsNullOrWhiteSpace(email)) fields.Add("email");
            if (string.IsNullOrEmpty(password)) fields.Add("password");

            throw new AuthException(
                "AUTH_REQUIRED_FIELDS",
                "Vui lòng nhập email và mật khẩu.",
                fields.ToArray());
        }

        if (email.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            throw new AuthException(
                "AUTH_INVALID_EMAIL",
                "Email không đúng định dạng.",
                "email");
        }

        if (password.Length > 256)
        {
            throw new AuthException(
                "AUTH_INVALID_REQUEST",
                "Mật khẩu vượt quá độ dài cho phép.",
                "password");
        }

        var user = await _dbContext.Users
            .Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new AuthException(
                "AUTH_INVALID_CREDENTIALS",
                "Email hoặc mật khẩu không chính xác.");
        }

        if (!user.IsActive)
        {
            throw new AuthException(
                "AUTH_ACCOUNT_INACTIVE",
                "Tài khoản đã bị vô hiệu hóa.");
        }

        var issuedAt = DateTimeOffset.UtcNow;
        var accessToken = CreateAccessToken(user, issuedAt, out var accessExpiresAt);
        var refreshToken = CreateRefreshToken();
        var refreshExpiresAt = issuedAt.Add(RefreshTokenLifetime);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        _dbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RefreshToken = refreshToken,
            ExpiresAt = refreshExpiresAt,
            IsRevoked = false,
            CreatedAt = issuedAt
        });

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new Response.LoginResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            FullName = user.FullName
        };
    }

    public async Task<Response.RefreshResponse> RefreshAsync(Request.RefreshRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException(
                "AUTH_REQUIRED_REFRESH_TOKEN",
                "Refresh token là bắt buộc.",
                "refreshToken");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var session = await _dbContext.UserSessions
            .Include(x => x.User)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.RefreshToken == request.RefreshToken);

        var now = DateTimeOffset.UtcNow;
        if (session is null || session.IsRevoked || session.ExpiresAt <= now || !session.User.IsActive)
        {
            throw new AuthException(
                "AUTH_INVALID_REFRESH_TOKEN",
                "Refresh token không hợp lệ hoặc đã hết hạn.");
        }

        var accessToken = CreateAccessToken(session.User, now, out var accessExpiresAt);
        var refreshToken = CreateRefreshToken();
        var refreshExpiresAt = now.Add(RefreshTokenLifetime);

        session.IsRevoked = true;
        session.UpdatedAt = now;
        _dbContext.UserSessions.Add(new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            RefreshToken = refreshToken,
            ExpiresAt = refreshExpiresAt,
            IsRevoked = false,
            CreatedAt = now
        });

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new Response.RefreshResponse
        {
            AccessToken = accessToken,
            TokenType = "Bearer",
            ExpiresAt = accessExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
    }

    public async Task LogoutAsync(Request.LogoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException(
                "AUTH_REQUIRED_REFRESH_TOKEN",
                "Refresh token là bắt buộc.",
                "refreshToken");
        }

        var session = await _dbContext.UserSessions
            .SingleOrDefaultAsync(x => x.RefreshToken == request.RefreshToken);

        if (session is null || session.IsRevoked)
        {
            return;
        }

        session.IsRevoked = true;
        session.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private string CreateAccessToken(User user, DateTimeOffset issuedAt, out DateTimeOffset expiresAt)
    {
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var configuredExpirationMinutes)
            ? configuredExpirationMinutes
            : 0;

        var options = new JwtOptions
        {
            SecretKey = _configuration["Jwt:Key"] ?? string.Empty,
            Issuer = _configuration["Jwt:Issuer"] ?? string.Empty,
            Audience = _configuration["Jwt:Audience"] ?? string.Empty,
            ExpirationMinutes = expirationMinutes
        };

        if (string.IsNullOrWhiteSpace(options.SecretKey) ||
            string.IsNullOrWhiteSpace(options.Issuer) ||
            string.IsNullOrWhiteSpace(options.Audience) ||
            options.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT configuration is incomplete.");
        }

        expiresAt = issuedAt.AddMinutes(options.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        if (user.Role is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, user.Role.Type));
        }

        return VNZ.Service.JwtService.JwtService.GenerateAccessToken(claims, options);
    }

    private static string CreateRefreshToken()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
    }
}
