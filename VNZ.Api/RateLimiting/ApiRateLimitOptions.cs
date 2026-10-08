namespace VNZ.Api.RateLimiting;

public sealed class ApiRateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public SlidingWindowPolicyOptions GlobalAnonymous { get; set; } = new(120, 1);
    public SlidingWindowPolicyOptions GlobalAuthenticated { get; set; } = new(300, 1);
    public SlidingWindowPolicyOptions Login { get; set; } = new(10, 5);
    public SlidingWindowPolicyOptions ForgotPassword { get; set; } = new(3, 15);
    public SlidingWindowPolicyOptions ChangePassword { get; set; } = new(10, 15);
    public SlidingWindowPolicyOptions ContactInquiryCreate { get; set; } = new(5, 10);
    public SlidingWindowPolicyOptions JobApplicationCreate { get; set; } = new(5, 10);
    public SlidingWindowPolicyOptions AdminUpload { get; set; } = new(20, 1);
    public SlidingWindowPolicyOptions AdminEmail { get; set; } = new(10, 1);

    public bool IsValid()
    {
        var policies = new[]
        {
            GlobalAnonymous, GlobalAuthenticated, Login, ForgotPassword,
            ChangePassword, ContactInquiryCreate, JobApplicationCreate,
            AdminUpload, AdminEmail
        };

        return policies.All(policy => policy is not null
            && policy.PermitLimit > 0
            && policy.SegmentsPerWindow > 0
            && policy.Window.Ticks / policy.SegmentsPerWindow >= TimeSpan.TicksPerMillisecond);
    }
}

public sealed class SlidingWindowPolicyOptions
{
    public int PermitLimit { get; set; }
    public TimeSpan Window { get; set; }
    public int SegmentsPerWindow { get; set; } = 6;

    public SlidingWindowPolicyOptions()
    {
    }

    public SlidingWindowPolicyOptions(int permitLimit, int windowMinutes)
    {
        PermitLimit = permitLimit;
        Window = TimeSpan.FromMinutes(windowMinutes);
    }
}
