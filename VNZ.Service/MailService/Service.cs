using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace VNZ.Service.MailService;

public class Service : IService
{
    public MailOptions _mailOptions = new();

    public Service(IConfiguration configuration)
    {
        var mailOptions = configuration.GetSection("MailOptions");
        _mailOptions.Mail = mailOptions["Mail"] ?? string.Empty;
        _mailOptions.DisplayName = mailOptions["DisplayName"] ?? string.Empty;
        _mailOptions.Password = mailOptions["Password"] ?? string.Empty;
        _mailOptions.Host = mailOptions["Host"] ?? string.Empty;
        _mailOptions.Port = int.TryParse(mailOptions["Port"], out var port) ? port : 0;
    }

    public async Task SendMail(MailContent mailContent)
    {
        MimeMessage email = new();
        email.Sender = new MailboxAddress(_mailOptions.DisplayName, _mailOptions.Mail);
        email.From.Add(new MailboxAddress(_mailOptions.DisplayName, _mailOptions.Mail));
        email.To.Add(MailboxAddress.Parse(mailContent.To));
        email.Subject = mailContent.Subject;

        BodyBuilder builder = new();
        builder.HtmlBody = mailContent.Body;
        email.Body = builder.ToMessageBody();

        using SmtpClient smtp = new();

        var socketOption = _mailOptions.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await smtp.ConnectAsync(_mailOptions.Host, _mailOptions.Port, socketOption);
        await smtp.AuthenticateAsync(_mailOptions.Mail, _mailOptions.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
