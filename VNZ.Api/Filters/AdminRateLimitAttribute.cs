using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using VNZ.Api.RateLimiting;
using JobApplicationRequest = VNZ.Service.JobApplicationService.Request;

namespace VNZ.Api.Filters;

public enum AdminRateLimitKind
{
    Upload,
    Email,
    RejectionEmail
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class AdminRateLimitAttribute : TypeFilterAttribute
{
    public AdminRateLimitAttribute(AdminRateLimitKind kind)
        : base(typeof(AdminRateLimitFilter))
    {
        Arguments = new object[] { kind };
        // Run after binding/model-state checks, before bilingual/business validation.
        // Malformed bodies keep their existing 400/415 response and are not reread.
        Order = -1000;
    }
}

public sealed class AdminRateLimitFilter : IAsyncActionFilter
{
    private readonly AdminRateLimitKind _kind;
    private readonly ApiRateLimiters _limiters;

    public AdminRateLimitFilter(AdminRateLimitKind kind, ApiRateLimiters limiters)
    {
        _kind = kind;
        _limiters = limiters;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        if (!ShouldLimit(context))
        {
            await next();
            return;
        }

        var upload = _kind == AdminRateLimitKind.Upload;
        using var lease = _limiters.AcquireAdmin(context.HttpContext, upload);

        if (!lease.IsAcquired)
        {
            await RateLimitResponse.WriteAsync(
                context.HttpContext,
                lease,
                _limiters.GetAdminWindow(upload),
                context.HttpContext.RequestAborted);
            context.Result = new EmptyResult();
            return;
        }

        await next();
    }

    private bool ShouldLimit(ActionExecutingContext context)
    {
        if (_kind == AdminRateLimitKind.Upload)
        {
            var request = context.HttpContext.Request;
            return request.HasFormContentType && request.Form.Files.Count > 0;
        }

        if (_kind == AdminRateLimitKind.RejectionEmail)
        {
            var request = context.ActionArguments.Values
                .OfType<JobApplicationRequest.ReviewJobApplicationRequest>()
                .FirstOrDefault();

            return string.Equals(request?.Decision?.Trim(), "Rejected", StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
