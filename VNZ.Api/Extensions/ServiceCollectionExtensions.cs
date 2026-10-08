using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text.Json;

using System.Security.Claims;

using System.Text;
using System.Text.Json.Serialization;

using VNZ.Api.BackgroundJob;
using VNZ.Api.Filters;

using VNZ.Api.Middleware;
using VNZ.Repository;
using VNZ.Service.Models;
using MailService = VNZ.Service.MailService;

namespace VNZ.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()
            .AddMvcOptions(options =>
            {
                options.Filters.Add<BilingualFormFieldValidationFilter>();
                options.Filters.Add<ApiResponseErrorResultFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var modelStateErrors = context.ModelState
                        .Where(item => item.Value?.Errors.Count > 0)
                        .SelectMany(item => item.Value!.Errors.Select(error =>
                            (Key: item.Key, Error: error.ErrorMessage)))
                        .ToArray();

                    var fields = modelStateErrors
                        .Select(item => NormalizeModelStateField(item.Key, item.Error))
                        .Where(field => !string.IsNullOrWhiteSpace(field))
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();

                    var isBilingualAdminEndpoint = IsBilingualAdminEndpoint(context.HttpContext);
                    var isPublicContactEndpoint = context.HttpContext.Request.Path.Equals(
                        "/api/v1/public/contacts",
                        StringComparison.OrdinalIgnoreCase);
                    var hasBilingualJsonIssue = modelStateErrors.Any(item =>
                        item.Error.Contains("could not be mapped", StringComparison.OrdinalIgnoreCase) ||
                        item.Error.Contains("JSON", StringComparison.OrdinalIgnoreCase) ||
                        IsBilingualModelStateField(NormalizeModelStateField(item.Key, item.Error)));

                    var errorCode = isPublicContactEndpoint
                        ? "CONTACT_CREATE_VALIDATION_FAILED"
                        : isBilingualAdminEndpoint && hasBilingualJsonIssue
                            ? "BILINGUAL_SCHEMA_INVALID"
                            : "BINDING_INVALID";

                    return new BadRequestObjectResult(ResponseBuilder.ErrorResponse(
                        errors: new ApiError
                        {
                            Code = errorCode,
                            Fields = fields
                        },
                        message: "Dữ liệu gửi lên không hợp lệ.",
                        traceId: context.HttpContext.TraceIdentifier));
                };
            });
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddTransient<GlobalExceptionHandlerMiddleware>();
        services.AddScoped<PublicContactApiResultFilter>();
        services.AddScoped<ApiResponseErrorResultFilter>();
        services.AddHostedService<JobPostExpirationBackgroundService>();
        services.AddScoped<VNZ.Service.AuthService.IService, VNZ.Service.AuthService.Service>();
        services.AddScoped<VNZ.Service.DashboardService.IService, VNZ.Service.DashboardService.Service>();
        services.AddScoped<VNZ.Service.ContactService.IService, VNZ.Service.ContactService.Service>();
        services.AddScoped<VNZ.Service.JobPostService.IService, VNZ.Service.JobPostService.Service>();
        services.AddScoped<VNZ.Service.DepartmentService.IService, VNZ.Service.DepartmentService.Service>();
        services.AddScoped<VNZ.Service.NewsService.IService, VNZ.Service.NewsService.Service>();
        services.AddScoped<VNZ.Service.ProductService.IService, VNZ.Service.ProductService.Service>();
        services.AddScoped<VNZ.Service.Localization.IBilingualIntegrityChecker,
            VNZ.Service.Localization.BilingualIntegrityChecker>();
        services.AddScoped<VNZ.Service.PartnerService.IService, VNZ.Service.PartnerService.Service>();
        services.AddScoped<VNZ.Service.JobApplicationService.IService, VNZ.Service.JobApplicationService.Service>();
        services.AddScoped<VNZ.Service.TeamMembers.IService, VNZ.Service.TeamMembers.Service>();
        services.Configure<VNZ.Service.Utils.CloudinaryService.CloudinaryOptions>(
            configuration.GetSection(nameof(VNZ.Service.Utils.CloudinaryService.CloudinaryOptions)));
        services.AddScoped<VNZ.Service.Utils.MediaService.IService, VNZ.Service.Utils.CloudinaryService.Service>();
        services.AddScoped<VNZ.Service.Utils.RichTextService.IService, VNZ.Service.Utils.RichTextService.Service>();
        services.AddScoped<VNZ.Service.Utils.SlugService.IService, VNZ.Service.Utils.SlugService.Service>();
        services.AddSingleton<MailService.IEmailTemplateRenderer, MailService.EmailTemplateRenderer>();
        services.AddHttpClient<MailService.IService, MailService.Service>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddSwaggerDocumentation();
        services.AddCorsPolicy();
        services.AddApiRateLimiting(configuration);

        return services;
    }

    private static bool IsBilingualAdminEndpoint(HttpContext context)
    {
        var path = context.Request.Path.Value;
        return path is not null &&
            (path.Equals("/api/v1/admin/news", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/api/v1/admin/news/", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/v1/admin/job-posts", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/api/v1/admin/job-posts/", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/v1/admin/products", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/api/v1/admin/products/", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeModelStateField(string key, string? errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            const string jsonPropertyPrefix = "The JSON property '";
            var prefixIndex = errorMessage.IndexOf(jsonPropertyPrefix, StringComparison.OrdinalIgnoreCase);

            if (prefixIndex >= 0)
            {
                var start = prefixIndex + jsonPropertyPrefix.Length;
                var end = errorMessage.IndexOf('\'', start);

                if (end > start)
                {
                    var property = errorMessage[start..end];
                    var normalizedKey = key.Trim();

                    if (normalizedKey.StartsWith("$.", StringComparison.Ordinal))
                    {
                        normalizedKey = normalizedKey[2..];
                    }

                    var path = string.IsNullOrWhiteSpace(normalizedKey) || normalizedKey == "$"
                        ? property
                        : $"{normalizedKey}.{property}";

                    return ConvertPathToCamelCase(path);
                }
            }
        }

        var normalized = key.Trim();
        normalized = normalized.StartsWith("$.", StringComparison.Ordinal)
            ? normalized[2..]
            : normalized;

        return ConvertPathToCamelCase(normalized);
    }

    private static bool IsBilingualModelStateField(string field)
    {
        return field.Equals("content", StringComparison.OrdinalIgnoreCase) ||
            field.StartsWith("content.", StringComparison.OrdinalIgnoreCase) ||
            field.Equals("translations", StringComparison.OrdinalIgnoreCase) ||
            field.StartsWith("translations.", StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertPathToCamelCase(string field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return field;
        }

        var segments = field.Split('.', StringSplitOptions.None);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];
            var bracketIndex = segment.IndexOf('[', StringComparison.Ordinal);
            var propertyName = bracketIndex < 0
                ? segment
                : segment[..bracketIndex];
            var suffix = bracketIndex < 0
                ? string.Empty
                : segment[bracketIndex..];

            segments[index] = JsonNamingPolicy.CamelCase.ConvertName(propertyName) + suffix;
        }

        return string.Join('.', segments);
    }

    // private static IServiceCollection AddDatabase(
    //     this IServiceCollection services,
    //     IConfiguration configuration)
    // {
    //     var connectionString = configuration.GetConnectionString("DefaultConnection");
    //
    //     services.AddDbContext<AppDbContext>(options =>
    //         options.UseNpgsql(connectionString, npgsqlOptions =>
    //         {
    //             npgsqlOptions.ConfigureDataSource(dataSourceBuilder =>
    //             {
    //                 dataSourceBuilder.EnableDynamicJson();
    //             });
    //         }));
    //     return services;
    // }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(dataSource));

        return services;
    }
    
    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"] ?? string.Empty;
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (!Guid.TryParse(userId, out var parsedUserId))
                        {
                            context.Fail("Token không chứa định danh người dùng hợp lệ.");
                            return;
                        }

                        var dbContext = context.HttpContext.RequestServices
                            .GetRequiredService<AppDbContext>();
                        var isActiveUser = await dbContext.Users
                            .AsNoTracking()
                            .AnyAsync(user => user.Id == parsedUserId && user.IsActive);

                        if (!isActiveUser)
                        {
                            context.Fail("Tài khoản không còn hoạt động.");
                        }
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteAuthenticationErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "AUTH_UNAUTHENTICATED",
                            "Yêu cầu đăng nhập để tiếp tục.");
                    },
                    OnForbidden = async context =>
                    {
                        await WriteAuthenticationErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status403Forbidden,
                            "AUTH_FORBIDDEN",
                            "Bạn không có quyền thực hiện thao tác này.");
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static async Task WriteAuthenticationErrorAsync(
        HttpContext context,
        int statusCode,
        string errorCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ResponseBuilder.ErrorResponse(
            errors: new
            {
                code = errorCode,
                fields = Array.Empty<string>()
            },
            message: message,
            traceId: context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "VNZ API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    private static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Retry-After");
            });
        });

        return services;
    }
}
