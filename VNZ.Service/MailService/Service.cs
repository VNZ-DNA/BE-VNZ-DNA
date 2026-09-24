using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace VNZ.Service.MailService;

public class Service : IService
{
    private readonly HttpClient _httpClient;
    private readonly MailOptions _mailOptions;

    public Service(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _mailOptions = new MailOptions
        {
            BaseUrl = configuration["BREVO_BASE_URL"] ?? "https://api.brevo.com/v3/",
            ApiKey = configuration["BREVO_API_KEY"] ?? string.Empty,
            SenderEmail = configuration["BREVO_SENDER_EMAIL"] ?? string.Empty,
            SenderName = configuration["BREVO_SENDER_NAME"] ?? string.Empty,
            ReplyToEmail = configuration["BREVO_REPLY_TO_EMAIL"] ?? string.Empty
        };
    }

    public async Task SendAsync(MailContent mailContent)
    {
        ValidateConfiguration();

        var payload = new
        {
            sender = new
            {
                name = _mailOptions.SenderName,
                email = _mailOptions.SenderEmail
            },
            to = new[]
            {
                new
                {
                    name = mailContent.ToName,
                    email = mailContent.To
                }
            },
            replyTo = new
            {
                email = _mailOptions.ReplyToEmail
            },
            subject = mailContent.Subject,
            htmlContent = mailContent.IsHtmlBody
                ? mailContent.Body
                : BuildContactReplyHtml(mailContent.ToName, mailContent.Body),
            headers = new Dictionary<string, string>
            {
                ["Idempotency-Key"] = mailContent.IdempotencyKey
            },
            tags = mailContent.IsHtmlBody ? null : new[] { "contact-reply" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_mailOptions.BaseUrl), "smtp/email"));
        request.Headers.Add("api-key", _mailOptions.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Brevo gửi email thất bại. HTTP {(int)response.StatusCode}: {responseBody}");
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_mailOptions.ApiKey)
            || string.IsNullOrWhiteSpace(_mailOptions.SenderEmail)
            || string.IsNullOrWhiteSpace(_mailOptions.SenderName)
            || string.IsNullOrWhiteSpace(_mailOptions.ReplyToEmail))
        {
            throw new InvalidOperationException("Cấu hình Brevo chưa đầy đủ.");
        }

        if (!Uri.TryCreate(_mailOptions.BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("BREVO_BASE_URL không hợp lệ.");
        }
    }

    private static string BuildContactReplyHtml(string recipientName, string body)
    {
        var encodedRecipientName = WebUtility.HtmlEncode(recipientName);
        var encodedBody = WebUtility.HtmlEncode(body)
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Replace("\n", "<br>");

        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Phản hồi từ VNZ</title>
            </head>
            <body style="margin:0;padding:24px;background:#f4f0fa;font-family:Arial,Helvetica,sans-serif;color:#243447;line-height:1.6;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:640px;margin:0 auto;background:#ffffff;border-collapse:collapse;">
                    <tr>
                        <td style="padding:24px;background:#f36b21;color:#ffffff;font-size:24px;font-weight:700;">VNZ Technology</td>
                    </tr>
                    <tr>
                        <td style="padding:32px 24px;">
                            <p>Xin chào {encodedRecipientName},</p>
                            <p>{encodedBody}</p>
                            <p>Nếu cần trao đổi thêm, bạn có thể phản hồi trực tiếp email này.</p>
                            <p>Trân trọng,<br>Đội ngũ VNZ</p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:16px 24px;background:#f4f0fa;color:#667085;font-size:12px;">VNZ Technology</td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
