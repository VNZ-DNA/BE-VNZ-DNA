namespace VNZ.Service.MailService;

public interface IService
{
    public Task SendMail(MailContent content);
}

public class MailContent
{
    public required string To { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
}
