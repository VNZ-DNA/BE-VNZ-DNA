namespace VNZ.Service.MailService;

public class MailOptions
{
    public string BaseUrl { get; set; } = "https://api.brevo.com/v3/";
    public string ApiKey { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string ReplyToEmail { get; set; } = string.Empty;
}
