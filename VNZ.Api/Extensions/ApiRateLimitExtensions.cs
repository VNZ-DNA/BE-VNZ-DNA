using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VNZ.Api.RateLimiting;

namespace VNZ.Api.Extensions;

public static class ApiRateLimitExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApiRateLimitOptions>()
            .Bind(configuration.GetSection(ApiRateLimitOptions.SectionName))
            .Validate(options => options.IsValid(),
                "RateLimiting phải có PermitLimit dương và segment dài ít nhất 1 ms.")
            .ValidateOnStart();
        services.AddSingleton<ApiRateLimiters>();
        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<ApiRateLimiters, IOptions<ApiRateLimitOptions>>((options, limiters, settings) =>
            {
                var policies = settings.Value;
                options.GlobalLimiter = limiters.GlobalLimiter;
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(RateLimitPolicies.Login,
                    context => limiters.CreateIpPartition(context, policies.Login));
                options.AddPolicy(RateLimitPolicies.ForgotPassword,
                    context => limiters.CreateIpPartition(context, policies.ForgotPassword));
                options.AddPolicy(RateLimitPolicies.ChangePassword,
                    context => limiters.CreateIpPartition(context, policies.ChangePassword));
                options.AddPolicy(RateLimitPolicies.ContactInquiryCreate,
                    limiters.CreateContactPartition);
                options.AddPolicy(RateLimitPolicies.JobApplicationCreate,
                    context => limiters.CreateIpPartition(context, policies.JobApplicationCreate));
                options.OnRejected = (context, cancellationToken) =>
                {
                    var window = (TimeSpan)context.HttpContext.Items[RateLimitResponse.WindowKey]!;
                    return RateLimitResponse.WriteAsync(
                        context.HttpContext, context.Lease, window, cancellationToken);
                };
            });

        return services;
    }
}
