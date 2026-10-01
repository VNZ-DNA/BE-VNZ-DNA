using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using VNZ.Service.JwtService;
using MailService = VNZ.Service.MailService;

namespace VNZ.Service.AuthService;

public class Service : IService
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(3);

    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly MailService.IService _mailService;

    public Service(
        AppDbContext dbContext,
        IConfiguration configuration,
        MailService.IService mailService)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _mailService = mailService;
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

        if (user is null ||
            string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
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
        if (session is null || session.User is null || session.IsRevoked ||
            session.ExpiresAt <= now || !session.User.IsActive)
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

    public async Task<Response.RegisterResponse> Register(Request.RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Vui lòng nhập họ và tên.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Vui lòng nhập email.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Vui lòng nhập mật khẩu.");
        }

        if (request.Password.Length < 6)
        {
            throw new ArgumentException("Mật khẩu phải có ít nhất 6 ký tự.");
        }

        var emailExist = await _dbContext.Users.AnyAsync(x => x.Email.ToLower() == email);
        if (emailExist)
        {
            throw new ArgumentException("Email đã tồn tại trong hệ thống.");
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(x => x.Type == "Admin");
        if (role == null)
        {
            throw new InvalidOperationException("Không tìm thấy quyền Admin trong hệ thống.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId = role.Id,
            Role = role,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = false,
            CreateAt = DateTimeOffset.UtcNow,
            ResetPasswordCode = 0
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new Response.RegisterResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.Type
        };
    }

    public async Task<string> ForgotPassword(Request.ForgotPasswordRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x =>
            x.Email == request.Email && x.IsActive);

        if (user == null)
        {
            throw new ArgumentException("Email không tồn tại trong hệ thống.");
        }

        var resetCode = new Random().Next(100000, 999999);
        user.ResetPasswordCode = resetCode;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        await _mailService.SendAsync(new MailService.MailContent
        {
            To = request.Email,
            ToName = user.FullName,
            Subject = "VNZ DNA - Quên mật khẩu",
            Body = BuildVerificationEmailBody(user.FullName, resetCode),
            IdempotencyKey = $"forgot-password-{user.Id}-{resetCode}",
            IsHtmlBody = true
        });

        return "Vui lòng kiểm tra email để nhận mã đặt lại mật khẩu.";
    }

    public async Task<string> ChangePassword(Request.ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Vui lòng nhập mật khẩu mới.");
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => request.Code == x.ResetPasswordCode);
        if (user == null) throw new ArgumentException("Mã đặt lại mật khẩu không hợp lệ.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.ResetPasswordCode = 0;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
    }

    private static string BuildVerificationEmailBody(string fullName, int verifiedCode) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
            <title>VNZ DNA - Quên mật khẩu</title>
        </head>
        <body style="margin:0;padding:24px;background:#F4F0FA;font-family:Arial,Helvetica,sans-serif;color:#243447;">
            <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" role="presentation"
                               style="max-width:600px;background:#ffffff;border-radius:28px;padding:40px;">
                            <tr>
                                <td>
                                    <h2 style="margin:0 0 20px;color:#9B5DE5;">VNZ DNA - Quên mật khẩu</h2>
                                    <p>Xin chào <strong>{System.Net.WebUtility.HtmlEncode(fullName)}</strong>,</p>
                                    <p>Vui lòng sử dụng mã xác nhận bên dưới để đổi mật khẩu.</p>
                                    <p style="margin:28px 0;text-align:center;font-size:36px;font-weight:800;letter-spacing:10px;color:#FF9F43;">
                                        {verifiedCode}
                                    </p>
                                    <p>Mã này có hiệu lực trong 5 phút. Nếu bạn không yêu cầu đổi mật khẩu, vui lòng bỏ qua email này.</p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;

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
            throw new InvalidOperationException("Cấu hình JWT chưa đầy đủ.");
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
