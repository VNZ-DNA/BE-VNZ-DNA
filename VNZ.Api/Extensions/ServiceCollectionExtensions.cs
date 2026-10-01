using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
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
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problemDetailsFactory = context.HttpContext.RequestServices
                        .GetRequiredService<ProblemDetailsFactory>();
                    var problemDetails = problemDetailsFactory.CreateValidationProblemDetails(
                        context.HttpContext,
                        context.ModelState,
                        statusCode: StatusCodes.Status400BadRequest);

                    problemDetails.Title = "Dữ liệu gửi lên không hợp lệ.";

                    return new BadRequestObjectResult(problemDetails);
                };
            });
        services.AddEndpointsApiExplorer();
        services.AddHttpContextAccessor();
        services.AddTransient<GlobalExceptionHandlerMiddleware>();
        services.AddScoped<PublicContactApiResultFilter>();
        services.AddHostedService<JobPostExpirationBackgroundService>();
        services.AddScoped<VNZ.Service.AuthService.IService, VNZ.Service.AuthService.Service>();
        services.AddScoped<VNZ.Service.DashboardService.IService, VNZ.Service.DashboardService.Service>();
        services.AddScoped<VNZ.Service.ContactService.IService, VNZ.Service.ContactService.Service>();
        services.AddScoped<VNZ.Service.JobPostService.IService, VNZ.Service.JobPostService.Service>();
        services.AddScoped<VNZ.Service.DepartmentService.IService, VNZ.Service.DepartmentService.Service>();
        services.AddScoped<VNZ.Service.NewsService.IService, VNZ.Service.NewsService.Service>();
        services.AddScoped<VNZ.Service.ProductService.IService, VNZ.Service.ProductService.Service>();
        services.AddScoped<VNZ.Service.PartnerService.IService, VNZ.Service.PartnerService.Service>();
        services.AddScoped<VNZ.Service.JobApplicationService.IService, VNZ.Service.JobApplicationService.Service>();
        services.AddScoped<VNZ.Service.TeamMembers.IService, VNZ.Service.TeamMembers.Service>();
        services.Configure<VNZ.Service.Utils.CloudinaryService.CloudinaryOptions>(
            configuration.GetSection(nameof(VNZ.Service.Utils.CloudinaryService.CloudinaryOptions)));
        services.AddScoped<VNZ.Service.Utils.MediaService.IService, VNZ.Service.Utils.CloudinaryService.Service>();
        services.AddScoped<VNZ.Service.Utils.RichTextService.IService, VNZ.Service.Utils.RichTextService.Service>();
        services.AddSingleton<MailService.IEmailTemplateRenderer, MailService.EmailTemplateRenderer>();
        services.AddHttpClient<MailService.IService, MailService.Service>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddDatabase(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddSwaggerDocumentation();
        services.AddCorsPolicy();
        services.AddContactInquiryRateLimit();

        return services;
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
