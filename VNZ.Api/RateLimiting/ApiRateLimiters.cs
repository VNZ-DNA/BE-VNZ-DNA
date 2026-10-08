using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using VNZ.Service.Exceptions;

namespace VNZ.Api.RateLimiting;

public sealed class ApiRateLimiters : IDisposable
{
    private readonly ApiRateLimitOptions _options;
    private readonly PartitionedRateLimiter<HttpContext> _uploadLimiter;
    private readonly PartitionedRateLimiter<HttpContext> _emailLimiter;

    public PartitionedRateLimiter<HttpContext> GlobalLimiter { get; }

    public ApiRateLimiters(IOptions<ApiRateLimitOptions> options)
    {
        _options = options.Value;
        GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(CreateGlobalPartition);
        _uploadLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            CreatePartition(GetIdentityKey(context), _options.AdminUpload));
        _emailLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            CreatePartition(GetIdentityKey(context), _options.AdminEmail));
    }

    public RateLimitPartition<string> CreateIpPartition(
        HttpContext context,
        SlidingWindowPolicyOptions policy)
    {
        // Global runs first, then the named policy. The last attempted partition
        // supplies the fallback for the limiter which actually rejects the request.
        context.Items[RateLimitResponse.WindowKey] = policy.Window;
        return CreatePartition(GetIpKey(context), policy);
    }

    public RateLimitPartition<string> CreateContactPartition(HttpContext context)
    {
        if (context.Connection.RemoteIpAddress is null)
        {
            throw new ContactException(
                "CONTACT_CREATE_FAILED",
                "Không thể tiếp nhận yêu cầu liên hệ lúc này. Vui lòng thử lại sau.");
        }

        return CreateIpPartition(context, _options.ContactInquiryCreate);
    }

    public RateLimitLease AcquireAdmin(HttpContext context, bool upload)
    {
        if (upload)
        {
            return _uploadLimiter.AttemptAcquire(context);
        }

        return _emailLimiter.AttemptAcquire(context);
    }

    public TimeSpan GetAdminWindow(bool upload)
    {
        if (upload)
        {
            return _options.AdminUpload.Window;
        }

        return _options.AdminEmail.Window;
    }

    public void Dispose()
    {
        GlobalLimiter.Dispose();
        _uploadLimiter.Dispose();
        _emailLimiter.Dispose();
    }

    private RateLimitPartition<string> CreateGlobalPartition(HttpContext context)
    {
        var isController = context.GetEndpoint()?.Metadata
            .GetMetadata<ControllerActionDescriptor>() is not null;

        if (!isController || !context.Request.Path.StartsWithSegments("/api/v1"))
        {
            return RateLimitPartition.GetNoLimiter("outside-api");
        }

        var userKey = GetUserKey(context);
        var policy = _options.GlobalAnonymous;
        var partitionKey = GetIpKey(context);

        if (userKey is not null)
        {
            policy = _options.GlobalAuthenticated;
            partitionKey = userKey;
        }

        context.Items[RateLimitResponse.WindowKey] = policy.Window;
        return CreatePartition(partitionKey, policy);
    }

    private static RateLimitPartition<string> CreatePartition(
        string partitionKey,
        SlidingWindowPolicyOptions policy)
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = policy.Window,
                SegmentsPerWindow = policy.SegmentsPerWindow,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    private static string GetIdentityKey(HttpContext context)
    {
        return GetUserKey(context) ?? GetIpKey(context);
    }

    private static string? GetUserKey(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        return $"user:{parsedUserId:D}";
    }

    private static string GetIpKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;

        if (address is null)
        {
            return "ip:unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return $"ip:{address}";
    }
}
