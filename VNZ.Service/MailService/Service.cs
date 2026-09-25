using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace VNZ.Service.MailService;

public class Service : IService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private const string InterviewLocationMapUrl =
        "https://www.google.com/maps/place/C%C3%94NG+TY+TNHH+KOKEN/@10.8210167,106.7842329,1018m/data=!3m2!1e3!4b1!4m6!3m5!1s0x3175277517c8d095:0xa5a0955a7ad81fc3!8m2!3d10.8210167!4d106.7842329!16s%2Fg%2F11svb4wcm6!18m1!1e1?entry=ttu&g_ep=EgoyMDI2MDkyMi4wIKXMDSoASAFQAw%3D%3D";
    private const string VnzLogoUrl = "https://be-vnz-dna-latest.onrender.com/images/logo-dark.png";

    private readonly HttpClient _httpClient;
    private readonly MailOptions _mailOptions;
    private readonly ILogger<Service> _logger;

    public Service(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<Service> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

    public async Task<MailDeliveryResult> SendInterviewInvitationAsync(InterviewInvitationMailContent content)
    {
        try
        {
            await SendAsync(new MailContent
            {
                To = content.To,
                ToName = content.ToName,
                Subject = "Thư mời phỏng vấn tại VNZ",
                Body = BuildInterviewInvitationHtml(content.PositionTitle, content.InterviewAt),
                IdempotencyKey = content.IdempotencyKey,
                IsHtmlBody = true
            });

            return new MailDeliveryResult
            {
                IsSuccess = true
            };
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Unable to send interview invitation. ApplicationId: {ApplicationId}",
                content.ApplicationId);
        }
        catch (TaskCanceledException exception)
        {
            _logger.LogWarning(
                exception,
                "Interview invitation timed out. ApplicationId: {ApplicationId}",
                content.ApplicationId);
        }

        return new MailDeliveryResult
        {
            IsSuccess = false
        };
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

    private static string BuildInterviewInvitationHtml(string positionTitle, DateTimeOffset interviewAt)
    {
        var localInterviewAt = interviewAt.ToOffset(VietnamOffset);
        var formattedInterviewAt = FormatInterviewAt(localInterviewAt);
        var confirmationDeadline = localInterviewAt.AddDays(-2).ToString(
            "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture);
        var encodedPositionTitle = WebUtility.HtmlEncode(positionTitle);

        return $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Thư mời phỏng vấn tại VNZ</title>
            </head>
            <body style="margin:0;padding:32px 12px;background:#eef2f6;font-family:Arial,Helvetica,sans-serif;color:#243447;font-size:14px;line-height:1.85;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;margin:0 auto;background:#ffffff;border-collapse:separate;border-spacing:0;border-radius:6px;">
                    <tr>
                        <td align="center" style="padding:36px 40px 24px;">
                            <img src="{VnzLogoUrl}" width="200" alt="VNZ Technology" style="display:block;width:200px;max-width:100%;height:auto;border:0;">
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:0 40px;">
                            <div style="height:1px;background:#dbe2ea;"></div>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:26px 40px 38px;">
                            <p style="margin:0 0 18px;">Thân chào bạn,</p>
                            <p style="margin:0 0 18px;">Cảm ơn bạn đã quan tâm và ứng tuyển chương trình thực tập (OJT) tại <strong>VNZ Technology</strong> thông qua chương trình hợp tác giữa công ty và <strong>Đại học FPT TP.HCM.</strong></p>
                            <p style="margin:0 0 20px;">Sau khi xem xét hồ sơ, chúng tôi trân trọng mời bạn tham gia buổi phỏng vấn cho vị trí <strong>{encodedPositionTitle}</strong>, kỳ <strong>Fall 2026.</strong></p>

                            <p style="margin:0 0 12px;">Thông tin buổi phỏng vấn:</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;font-size:14px;line-height:1.6;">
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">Vị trí thực tập</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;">{encodedPositionTitle}</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">Thời gian</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;color:#e83e00;font-weight:700;">{formattedInterviewAt}</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">Hình thức</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;">Trực tiếp tại văn phòng</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">Địa điểm / Link</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;"><a href="{InterviewLocationMapUrl}" style="color:#1769aa;text-decoration:underline;">84 đường D5</a>, KDC Thiên Lý, Phường Phước Long, TP. HCM</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;color:#64748b;">Thời lượng dự kiến</td>
                                    <td style="padding:12px 14px;">30 phút</td>
                                </tr>
                            </table>

                            <p style="margin:24px 0 10px;font-weight:700;color:#1f2937;">Nội dung buổi phỏng vấn</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;">
                                <tr>
                                    <td style="padding:0 0 14px 14px;border-left:3px solid #f36b21;">
                                        <p style="margin:0 0 4px;">1. Giới thiệu &amp; định hướng</p>
                                        <p style="margin:0;color:#64748b;">Trao đổi về bản thân, định hướng nghề nghiệp và mong muốn của bạn trong kỳ thực tập.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:0 0 14px 14px;border-left:3px solid #f36b21;">
                                        <p style="margin:0 0 4px;">2. Kiến thức chuyên môn</p>
                                        <p style="margin:0;color:#64748b;">Một số câu hỏi cơ bản liên quan đến vị trí ứng tuyển và các môn học, dự án bạn đã thực hiện tại trường.</p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:0 0 0 14px;border-left:3px solid #f36b21;">
                                        <p style="margin:0 0 4px;">3. Giới thiệu công ty &amp; hỏi đáp</p>
                                        <p style="margin:0;color:#64748b;">Chia sẻ về VNZ Technology, lộ trình thực tập, và giải đáp các thắc mắc của bạn.</p>
                                    </td>
                                </tr>
                            </table>

                            <p style="margin:24px 0 10px;font-weight:700;color:#1f2937;">Bạn cần chuẩn bị</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;background:#f3f6f9;">
                                <tr><td style="padding:12px 18px;color:#64748b;">•&nbsp; CV bản cứng hoặc bản mềm (nếu có cập nhật mới)<br>•&nbsp; Thẻ sinh viên<br>•&nbsp; Sản phẩm, dự án hoặc portfolio bạn muốn giới thiệu (nếu có)<br>•&nbsp; Có mặt trước giờ hẹn khoảng 10 phút để chuẩn bị</td></tr>
                            </table>

                            <p style="margin:24px 0 12px;">Vui lòng phản hồi email này để xác nhận tham gia trước <strong>14:00 ngày {confirmationDeadline}</strong>. Nếu thời gian trên không phù hợp, bạn có thể đề xuất khung giờ khác để chúng tôi sắp xếp lại.</p>
                            <p style="margin:0 0 4px;">Chúc bạn có một buổi phỏng vấn thật thoải mái. Rất mong được gặp bạn.</p>
                            <p style="margin:0;">Trân trọng,<br><strong>VNZ Technology</strong></p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    private static string FormatInterviewAt(DateTimeOffset interviewAt)
    {
        var weekday = interviewAt.DayOfWeek switch
        {
            DayOfWeek.Sunday => "Chủ nhật",
            DayOfWeek.Monday => "Thứ hai",
            DayOfWeek.Tuesday => "Thứ ba",
            DayOfWeek.Wednesday => "Thứ tư",
            DayOfWeek.Thursday => "Thứ năm",
            DayOfWeek.Friday => "Thứ sáu",
            DayOfWeek.Saturday => "Thứ bảy",
            _ => throw new ArgumentOutOfRangeException(nameof(interviewAt))
        };

        var formattedTime = interviewAt.ToString(
            "HH:mm",
            System.Globalization.CultureInfo.InvariantCulture);
        var formattedDate = interviewAt.ToString(
            "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture);

        return $"{formattedTime} - {weekday}, ngày {formattedDate}";
    }
}
