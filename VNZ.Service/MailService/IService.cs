namespace VNZ.Service.MailService;

public interface IService
{
    Task SendAsync(MailContent content);
    Task<MailDeliveryResult> SendInterviewInvitationAsync(InterviewInvitationMailContent content);
    Task<MailDeliveryResult> SendRejectionEmailAsync(RejectionEmailMailContent content);
}

public class MailContent
{
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsHtmlBody { get; set; }
    public string? Tag { get; set; }
}

public class InterviewInvitationMailContent
{
    public Guid ApplicationId { get; set; }
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public DateTimeOffset InterviewAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string InterviewMode { get; set; } = "Onsite";
    public string? Location { get; set; } = EmailTemplateDefaults.DefaultInterviewAddress;
    public string? LocationUrl { get; set; } = EmailTemplateDefaults.DefaultInterviewLocationUrl;
    public string? InterviewInformationHtml { get; set; }
    public string AgendaHtml { get; set; } = EmailTemplateDefaults.DefaultAgendaHtml;
    public string PreparationHtml { get; set; } = EmailTemplateDefaults.DefaultPreparationHtml;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class RejectionEmailMailContent
{
    public Guid ApplicationId { get; set; }
    public string To { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class MailDeliveryResult
{
    public bool IsSuccess { get; set; }
}
