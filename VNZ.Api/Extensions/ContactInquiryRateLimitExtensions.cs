using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using VNZ.Service.Exceptions;
using VNZ.Service.Models;

namespace VNZ.Api.Extensions;

public static class ContactInquiryRateLimitExtensions
{
    private const string PolicyName = "ContactInquiryCreate";
    private const int PermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    public static IServiceCollection AddContactInquiryRateLimit(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(PolicyName, httpContext =>
            {
                var clientIp = GetClientIpAddress(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    clientIp,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds = 600;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    retryAfterSeconds = Math.Max(
                        1,
                        (int)Math.Ceiling(retryAfter.TotalSeconds));
                }

                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                response.ContentType = "application/json";
                response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                var payload = ResponseBuilder.ErrorResponse(
                    errors: new
                    {
                        code = "CONTACT_RATE_LIMITED",
                        fields = Array.Empty<string>()
                    },
                    message: "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
                    traceId: context.HttpContext.TraceIdentifier);

                await response.WriteAsJsonAsync(payload, cancellationToken);
            };
        });

        return services;
    }

    public static WebApplication UseReverseProxyForwardedHeaders(
        this WebApplication app)
    {
        var reverseProxyEnabled = app.Configuration.GetValue<bool>("ReverseProxy:Enabled");

        if (!reverseProxyEnabled)
        {
            return app;
        }

        var forwardLimit = app.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit");
        var trustForwardedHeaders = app.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders");

        if (!forwardLimit.HasValue || forwardLimit.Value < 1)
        {
            throw new InvalidOperationException(
                "ReverseProxy:ForwardLimit phải là số nguyên dương khi ReverseProxy:Enabled=true.");
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = forwardLimit.Value
        };

        if (trustForwardedHeaders)
        {
            // Render chỉ cho public traffic đi qua ingress của nền tảng. Khi cờ này bật,
            // service tin header do ingress chuyển tiếp để lấy IP khách thật cho rate limit.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        }
        else
        {
            var knownProxies = app.Configuration["ReverseProxy:KnownProxies"];

            if (string.IsNullOrWhiteSpace(knownProxies))
            {
                throw new InvalidOperationException(
                    "ReverseProxy:KnownProxies phải được cấu hình khi không tin tất cả forwarded headers.");
            }

            var proxyAddresses = knownProxies.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var proxyAddress in proxyAddresses)
            {
                if (!IPAddress.TryParse(proxyAddress, out var ipAddress))
                {
                    throw new InvalidOperationException(
                        "ReverseProxy:KnownProxies chứa địa chỉ IP không hợp lệ.");
                }

                options.KnownProxies.Add(ipAddress);
            }
        }

        app.UseForwardedHeaders(options);

        return app;
    }

    private static string GetClientIpAddress(HttpContext httpContext)
    {
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress;

        if (remoteIpAddress is null)
        {
            throw new ContactException(
                "CONTACT_CREATE_FAILED",
                "Không thể tiếp nhận yêu cầu liên hệ lúc này. Vui lòng thử lại sau.");
        }

        if (remoteIpAddress.IsIPv4MappedToIPv6)
        {
            remoteIpAddress = remoteIpAddress.MapToIPv4();
        }

        return remoteIpAddress.ToString();
    }
}
