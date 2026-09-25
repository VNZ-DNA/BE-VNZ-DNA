namespace VNZ.Service.MailService;

public interface IService
{
    Task SendAsync(MailContent content);
    Task<MailDeliveryResult> SendInterviewInvitationAsync(InterviewInvitationMailContent content);
}

public class MailContent
{
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsHtmlBody { get; set; }
}

public class InterviewInvitationMailContent
{
    public Guid ApplicationId { get; set; }
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public DateTimeOffset InterviewAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class MailDeliveryResult
{
    public bool IsSuccess { get; set; }
}
