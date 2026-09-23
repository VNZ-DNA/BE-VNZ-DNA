using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VNZ.Api.BackgroundJob;
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
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddTransient<GlobalExceptionHandlerMiddleware>();
        services.AddHostedService<JobPostExpirationBackgroundService>();
        services.AddScoped<VNZ.Service.AuthService.IService, VNZ.Service.AuthService.Service>();
        services.AddScoped<VNZ.Service.DashboardService.IService, VNZ.Service.DashboardService.Service>();
        services.AddScoped<VNZ.Service.JobPostService.IService, VNZ.Service.JobPostService.Service>();
        services.AddScoped<MailService.IService, MailService.Service>();

        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddSwaggerDocumentation();
        services.AddCorsPolicy();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

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
                    .AllowAnyMethod();
            });
        });

        return services;
    }
}
