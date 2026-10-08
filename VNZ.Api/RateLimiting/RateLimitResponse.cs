using System.Globalization;
using System.Threading.RateLimiting;
using VNZ.Service.Models;

namespace VNZ.Api.RateLimiting;

public static class RateLimitResponse
{
    internal static readonly object WindowKey = new();

    public static async ValueTask WriteAsync(
        HttpContext httpContext,
        RateLimitLease lease,
        TimeSpan fallbackWindow,
        CancellationToken cancellationToken)
    {
        var retryAfter = fallbackWindow;

        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var metadataRetryAfter))
        {
            retryAfter = metadataRetryAfter;
        }

        var retryAfterSeconds = Math.Max(1, Math.Ceiling(retryAfter.TotalSeconds));
        var response = httpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        var payload = ResponseBuilder.ErrorResponse(
            errors: new
            {
                code = "RATE_LIMITED",
                fields = Array.Empty<string>()
            },
            message: "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau.",
            traceId: httpContext.TraceIdentifier);

        await response.WriteAsJsonAsync(payload, cancellationToken);
    }
}
