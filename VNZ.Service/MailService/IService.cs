namespace VNZ.Service.MailService;

public interface IService
{
    Task SendAsync(MailContent content);
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
