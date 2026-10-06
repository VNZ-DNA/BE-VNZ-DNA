using System.Net;
using System.Globalization;

namespace VNZ.Service.MailService;

public interface IEmailTemplateRenderer
{
    RenderedEmail RenderInterview(InterviewEmailTemplateData data);
    RenderedEmail RenderContact(ContactEmailTemplateData data);
    RenderedEmail RenderJobApplicationReceived(JobApplicationReceivedEmailTemplateData data);
}

public sealed class RenderedEmail
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
}

public sealed class InterviewEmailTemplateData
{
    public string CandidateName { get; init; } = string.Empty;
    public string PositionTitle { get; init; } = string.Empty;
    public DateTimeOffset InterviewAt { get; init; }
    public int DurationMinutes { get; init; } = 30;
    public string InterviewMode { get; init; } = "Onsite";
    public string? Location { get; init; } = EmailTemplateDefaults.DefaultInterviewAddress;
    public string? LocationUrl { get; init; } = EmailTemplateDefaults.DefaultInterviewLocationUrl;
    public string? InterviewInformationHtml { get; init; }
    public string AgendaHtml { get; init; } = EmailTemplateDefaults.DefaultAgendaHtml;
    public string PreparationHtml { get; init; } = EmailTemplateDefaults.DefaultPreparationHtml;
}

public sealed class ContactEmailTemplateData
{
    public string RecipientName { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string BodyHtml { get; init; } = string.Empty;
    public string? ProposalHtml { get; init; }
    public string? NextStepsHtml { get; init; }
}

public sealed class JobApplicationReceivedEmailTemplateData
{
    public string CandidateName { get; init; } = string.Empty;
    public string PositionTitle { get; init; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; init; }
}

internal static class EmailTemplateDefaults
{
    public const string VnzLogoUrl = "https://be-vnz-dna-latest.onrender.com/images/logo-dark.png";
    public const string PrimaryColor = "#f36b21";
    public const string FontFamily = "Arial, Helvetica, sans-serif";
    public const int ContentMaxWidthPx = 640;

    public const string InterviewSubject = "Thư mời phỏng vấn tại VNZ";
    public const string InterviewGreetingTemplate = "Thân chào {{candidate.fullName}},";
    public const string InterviewOpeningTemplate = "Cảm ơn bạn đã quan tâm và ứng tuyển tại VNZ Technology.\n\nSau khi xem xét hồ sơ, chúng tôi trân trọng mời bạn tham gia buổi phỏng vấn cho vị trí {{candidate.positionTitle}}.";
    public const string InterviewInformationHeading = "Thông tin buổi phỏng vấn";
    public const string InterviewAgendaHeading = "Nội dung buổi phỏng vấn";
    public const string InterviewPreparationHeading = "Bạn cần chuẩn bị";
    public const string InterviewConfirmationTemplate = "Vui lòng phản hồi email này để xác nhận tham gia trước {{deadline}}. Nếu thời gian trên không phù hợp, bạn có thể đề xuất khung giờ khác để chúng tôi sắp xếp lại.";
    public const string InterviewClosing = "Chúc bạn có một buổi phỏng vấn thật thoải mái. Rất mong được gặp bạn.\n\nTrân trọng,";
    public const string InterviewSignature = "VNZ Technology";

    public const string ContactEyebrow = "PHẢN HỒI TỪ VNZ TECHNOLOGY";
    public const string ContactHeading = "Cảm ơn Anh/Chị đã kết nối với VNZ";
    public const string ContactGreetingTemplate = "Thân chào {{contact.fullName}},";
    public const string ContactOpening = "VNZ Technology trân trọng cảm ơn Anh/Chị đã dành thời gian liên hệ và chia sẻ nhu cầu. Chúng tôi đã xem xét nội dung trao đổi và gửi phản hồi đến Anh/Chị bên dưới.";
    public const string ContactBodyHeading = "Trao đổi cùng đội ngũ VNZ";
    public const string ContactProposalHeading = "Phương án đề xuất";
    public const string ContactNextStepsHeading = "Bước tiếp theo";
    public const string ContactClosing = "Chúng tôi mong muốn được tiếp tục lắng nghe, trao đổi để hiểu rõ hơn mục tiêu của Anh/Chị và cùng tìm ra hướng hợp tác phù hợp. Anh/Chị có thể trả lời trực tiếp email này nếu muốn bổ sung thông tin hoặc hẹn một buổi trao đổi.";
    public const string ContactSignature = "Đội ngũ VNZ Technology";
    public const string ContactFooter = "VNZ Technology";
    public const string ContactFooterTagline = "Vietnamese Minds • Global Solutions";

    public const string JobApplicationReceivedSubject = "VNZ Technology đã nhận được hồ sơ ứng tuyển của bạn";
    public const string JobApplicationReceivedEyebrow = "THÔNG BÁO TIẾP NHẬN HỒ SƠ";
    public const string JobApplicationReceivedGreetingTemplate = "Thân chào {{candidate.fullName}},";
    public const string JobApplicationReceivedOpening = "Cảm ơn bạn đã tin tưởng và dành thời gian ứng tuyển tại VNZ Technology.";
    public const string JobApplicationReceivedConfirmation = "Chúng tôi xác nhận đã nhận được hồ sơ ứng tuyển của bạn cho vị trí:";
    public const string JobApplicationReceivedPositionLabel = "Vị trí ứng tuyển";
    public const string JobApplicationReceivedTimeLabel = "Thời gian tiếp nhận";
    public const string JobApplicationReceivedReviewCopy = "Hồ sơ của bạn đã được ghi nhận và sẽ được bộ phận tuyển dụng xem xét cẩn thận. Nếu hồ sơ phù hợp với yêu cầu của vị trí, VNZ Technology sẽ chủ động liên hệ để thông báo đến bạn về bước tiếp theo.";
    public const string JobApplicationReceivedInboxCopy = "Trong thời gian chờ đợi, bạn vui lòng kiểm tra hộp thư đến và thư mục thư rác để không bỏ lỡ thông tin từ chúng tôi.";
    public const string JobApplicationReceivedClosing = "Chúc bạn một ngày tốt lành và cảm ơn bạn đã quan tâm đến cơ hội tại VNZ Technology.";
    public const string JobApplicationReceivedSignature = "Đội ngũ VNZ Technology";

    public const string DefaultInterviewLocationUrl =
        "https://www.google.com/maps/place/C%C3%94NG+TY+TNHH+KOKEN/@10.8210167,106.7842329,1018m/data=!3m2!1e3!4b1!4m6!3m5!1s0x3175277517c8d095:0xa5a0955a7ad81fc3!8m2!3d10.8210167!4d106.7842329!16s%2Fg%2F11svb4wcm6!18m1!1e1?entry=ttu&g_ep=EgoyMDI2MDkyMi4wIKXMDSoASAFQAw%3D%3D";
    public const string DefaultInterviewAddress = "84 đường D5, KDC Thiên Lý, Phường Phước Long, TP. HCM";
    public const string DefaultAgendaHtml = "<p>1. Giới thiệu &amp; định hướng</p><p>Trao đổi về bản thân, định hướng nghề nghiệp và mong muốn của bạn trong kỳ thực tập.</p><p>2. Kiến thức chuyên môn</p><p>Một số câu hỏi cơ bản liên quan đến vị trí ứng tuyển và các môn học, dự án bạn đã thực hiện tại trường.</p><p>3. Giới thiệu công ty &amp; hỏi đáp</p><p>Chia sẻ về VNZ Technology, lộ trình thực tập, và giải đáp các thắc mắc của bạn.</p>";
    public const string DefaultPreparationHtml = "<p>•&nbsp; CV bản cứng hoặc bản mềm (nếu có cập nhật mới)</p><p>•&nbsp; Thẻ sinh viên</p><p>•&nbsp; Sản phẩm, dự án hoặc portfolio bạn muốn giới thiệu (nếu có)</p><p>•&nbsp; Có mặt trước giờ hẹn khoảng 10 phút để chuẩn bị</p>";
}

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public RenderedEmail RenderInterview(InterviewEmailTemplateData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var localInterviewAt = data.InterviewAt.ToOffset(VietnamOffset);
        var encodedCandidateName = WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(data.CandidateName) ? "bạn" : data.CandidateName);
        var encodedPositionTitle = WebUtility.HtmlEncode(data.PositionTitle);
        var encodedLocation = WebUtility.HtmlEncode(data.Location ?? string.Empty);
        var encodedLocationUrl = WebUtility.HtmlEncode(data.LocationUrl ?? string.Empty);
        var formattedInterviewAt = FormatInterviewAt(localInterviewAt);
        var confirmationDeadline = $"14:00 ngày {localInterviewAt.AddDays(-2):dd/MM/yyyy}";
        var isOnline = string.Equals(data.InterviewMode, "Online", StringComparison.Ordinal);
        var modeLabel = isOnline ? "Trực tuyến" : "Trực tiếp tại văn phòng";
        var locationLabel = isOnline ? "Tham gia trực tuyến" : encodedLocation;
        var locationHtml = string.IsNullOrWhiteSpace(data.LocationUrl)
            ? locationLabel
            : $"<a href=\"{encodedLocationUrl}\" style=\"color:#1769aa;text-decoration:underline;\">{locationLabel}</a>";
        var interviewInformation = RenderOptionalRichBlock(data.InterviewInformationHtml);
        var greeting = EmailTemplateDefaults.InterviewGreetingTemplate.Replace(
            "{{candidate.fullName}}",
            encodedCandidateName,
            StringComparison.Ordinal);
        var openingText = EmailTemplateDefaults.InterviewOpeningTemplate.Replace(
            "{{candidate.positionTitle}}",
            $"<strong>{encodedPositionTitle}</strong>",
            StringComparison.Ordinal)
            .Replace(
                "VNZ Technology.",
                "<strong>VNZ Technology.</strong>",
            StringComparison.Ordinal);
        var openingHtml = string.Join(
            string.Empty,
            openingText
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(paragraph => $"<p style=\"margin:0 0 18px;\">{paragraph}</p>"));
        var confirmationCopy = EmailTemplateDefaults.InterviewConfirmationTemplate.Replace(
            "{{deadline}}",
            $"<strong>{confirmationDeadline}</strong>",
            StringComparison.Ordinal);
        var closingParts = EmailTemplateDefaults.InterviewClosing.Split(
            "\n\n",
            StringSplitOptions.RemoveEmptyEntries);

        var html = $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Thư mời phỏng vấn tại VNZ</title>
            </head>
            <body style="margin:0;padding:32px 12px;background:#eef2f6;font-family:{EmailTemplateDefaults.FontFamily};color:#243447;font-size:14px;line-height:1.85;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:{EmailTemplateDefaults.ContentMaxWidthPx}px;margin:0 auto;background:#ffffff;border-collapse:separate;border-spacing:0;border-radius:6px;">
                    <tr>
                        <td align="center" style="padding:36px 40px 24px;">
                            <img src="{EmailTemplateDefaults.VnzLogoUrl}" width="200" alt="VNZ Technology" style="display:block;width:200px;max-width:100%;height:auto;border:0;">
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:0 40px;"><div style="height:1px;background:#dbe2ea;"></div></td>
                    </tr>
                    <tr>
                        <td style="padding:26px 40px 38px;">
                            <p style="margin:0 0 18px;font-size:14px;">{greeting}</p>
                            {openingHtml}

                            <p style="margin:0 0 12px;">{EmailTemplateDefaults.InterviewInformationHeading}:</p>
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
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;">{modeLabel}</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">Địa điểm / Link</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;">{locationHtml}</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;color:#64748b;">Thời lượng dự kiến</td>
                                    <td style="padding:12px 14px;">{data.DurationMinutes} phút</td>
                                </tr>
                            </table>
                            {interviewInformation}

                            <p style="margin:24px 0 10px;font-weight:700;color:#1f2937;">{EmailTemplateDefaults.InterviewAgendaHeading}</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;">
                                <tr>
                                    <td style="padding:0 0 0 14px;border-left:3px solid {EmailTemplateDefaults.PrimaryColor};color:#334155;">{data.AgendaHtml}</td>
                                </tr>
                            </table>

                            <p style="margin:24px 0 10px;font-weight:700;color:#1f2937;">{EmailTemplateDefaults.InterviewPreparationHeading}</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;background:#f3f6f9;">
                                <tr><td style="padding:12px 18px;color:#64748b;">{data.PreparationHtml}</td></tr>
                            </table>

                            <p style="margin:24px 0 12px;">{confirmationCopy}</p>
                            <p style="margin:0 0 4px;">{closingParts[0]}</p>
                            <p style="margin:0;">{closingParts[1]}<br><strong>{EmailTemplateDefaults.InterviewSignature}</strong></p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        return new RenderedEmail
        {
            Subject = EmailTemplateDefaults.InterviewSubject,
            HtmlBody = html
        };
    }

    public RenderedEmail RenderContact(ContactEmailTemplateData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var encodedRecipientName = WebUtility.HtmlEncode(data.RecipientName);
        var greeting = EmailTemplateDefaults.ContactGreetingTemplate.Replace(
            "{{contact.fullName}}",
            encodedRecipientName,
            StringComparison.Ordinal);
        var proposal = RenderOptionalContactSection(EmailTemplateDefaults.ContactProposalHeading, data.ProposalHtml);
        var nextSteps = RenderOptionalContactSection(EmailTemplateDefaults.ContactNextStepsHeading, data.NextStepsHtml);

        var html = $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>VNZ Technology | Phản hồi liên hệ</title>
            </head>
            <body style="margin:0;padding:32px 12px;background:#eef2f6;font-family:{EmailTemplateDefaults.FontFamily};color:#243447;font-size:14px;line-height:1.85;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:{EmailTemplateDefaults.ContentMaxWidthPx}px;margin:0 auto;background:#ffffff;border-collapse:separate;border-spacing:0;border-radius:6px;">
                    <tr>
                        <td align="center" style="padding:36px 40px 24px;"><img src="{EmailTemplateDefaults.VnzLogoUrl}" width="200" alt="VNZ Technology" style="display:block;width:200px;max-width:100%;height:auto;border:0;"></td>
                    </tr>
                    <tr><td style="padding:0 40px;"><div style="height:1px;background:#dbe2ea;"></div></td></tr>
                    <tr>
                        <td style="padding:28px 40px 36px;">
                            <p style="margin:0 0 8px;color:{EmailTemplateDefaults.PrimaryColor};font-size:11px;font-weight:700;letter-spacing:1.2px;">{EmailTemplateDefaults.ContactEyebrow}</p>
                            <h1 style="margin:0 0 22px;color:#243447;font-size:18px;font-weight:700;line-height:1.4;">{EmailTemplateDefaults.ContactHeading}</h1>
                            <p style="margin:0 0 16px;font-size:14px;">{greeting}</p>
                            <p style="margin:0 0 22px;">{EmailTemplateDefaults.ContactOpening}</p>
                            <p style="margin:0 0 10px;font-weight:700;color:#243447;">{EmailTemplateDefaults.ContactBodyHeading}</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;background:#f3f6f9;">
                                <tr><td style="padding:18px 20px;border-left:3px solid {EmailTemplateDefaults.PrimaryColor};color:#334155;line-height:1.8;">{data.BodyHtml}</td></tr>
                            </table>
                            {proposal}
                            {nextSteps}
                            <p style="margin:22px 0 16px;">{EmailTemplateDefaults.ContactClosing}</p>
                            <p style="margin:0;">Trân trọng,<br><strong style="color:#243447;">{EmailTemplateDefaults.ContactSignature}</strong></p>
                        </td>
                    </tr>
                    <tr><td style="padding:0 40px;"><div style="height:1px;background:#dbe2ea;"></div></td></tr>
                    <tr>
                        <td align="center" style="padding:18px 24px 22px;background:#f3f6f9;color:#64748b;font-size:12px;line-height:1.6;"><strong style="color:#334155;">{EmailTemplateDefaults.ContactFooter}</strong><br>{EmailTemplateDefaults.ContactFooterTagline.Replace("•", $"<span style=\"color:{EmailTemplateDefaults.PrimaryColor};\">&#8226;</span>", StringComparison.Ordinal)}</td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        return new RenderedEmail
        {
            Subject = data.Subject,
            HtmlBody = html
        };
    }

    public RenderedEmail RenderJobApplicationReceived(JobApplicationReceivedEmailTemplateData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var encodedCandidateName = WebUtility.HtmlEncode(data.CandidateName);
        var encodedPositionTitle = WebUtility.HtmlEncode(data.PositionTitle);
        var receivedAt = data.ReceivedAt
            .ToOffset(VietnamOffset)
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

        var greeting = EmailTemplateDefaults.JobApplicationReceivedGreetingTemplate.Replace(
            "{{candidate.fullName}}",
            encodedCandidateName,
            StringComparison.Ordinal);

        var html = $"""
            <!DOCTYPE html>
            <html lang="vi">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{EmailTemplateDefaults.JobApplicationReceivedSubject}</title>
            </head>
            <body style="margin:0;padding:32px 12px;background:#eef2f6;font-family:{EmailTemplateDefaults.FontFamily};color:#243447;font-size:14px;line-height:1.85;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:{EmailTemplateDefaults.ContentMaxWidthPx}px;margin:0 auto;background:#ffffff;border-collapse:separate;border-spacing:0;border-radius:6px;">
                    <tr>
                        <td align="center" style="padding:36px 40px 24px;"><img src="{EmailTemplateDefaults.VnzLogoUrl}" width="200" alt="VNZ Technology" style="display:block;width:200px;max-width:100%;height:auto;border:0;"></td>
                    </tr>
                    <tr><td style="padding:0 40px;"><div style="height:1px;background:#dbe2ea;"></div></td></tr>
                    <tr>
                        <td style="padding:28px 40px 36px;">
                            <p style="margin:0 0 8px;color:{EmailTemplateDefaults.PrimaryColor};font-size:11px;font-weight:700;letter-spacing:1.2px;">{EmailTemplateDefaults.JobApplicationReceivedEyebrow}</p>
                            <p style="margin:0 0 16px;font-size:14px;">{greeting}</p>
                            <p style="margin:0 0 16px;">{EmailTemplateDefaults.JobApplicationReceivedOpening}</p>
                            <p style="margin:0 0 12px;">{EmailTemplateDefaults.JobApplicationReceivedConfirmation}</p>
                            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;font-size:14px;line-height:1.6;">
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;border-bottom:1px solid #dfe5ec;color:#64748b;">{EmailTemplateDefaults.JobApplicationReceivedPositionLabel}</td>
                                    <td style="padding:12px 14px;border-bottom:1px solid #dfe5ec;">{encodedPositionTitle}</td>
                                </tr>
                                <tr>
                                    <td width="38%" style="padding:12px 14px;background:#f3f6f9;color:#64748b;">{EmailTemplateDefaults.JobApplicationReceivedTimeLabel}</td>
                                    <td style="padding:12px 14px;color:#e83e00;font-weight:700;">{receivedAt}</td>
                                </tr>
                            </table>
                            <p style="margin:22px 0 16px;">{EmailTemplateDefaults.JobApplicationReceivedReviewCopy}</p>
                            <p style="margin:0 0 16px;">{EmailTemplateDefaults.JobApplicationReceivedInboxCopy}</p>
                            <p style="margin:0 0 16px;">{EmailTemplateDefaults.JobApplicationReceivedClosing}</p>
                            <p style="margin:0;">Trân trọng,<br><strong style="color:#243447;">{EmailTemplateDefaults.JobApplicationReceivedSignature}</strong></p>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;

        return new RenderedEmail
        {
            Subject = EmailTemplateDefaults.JobApplicationReceivedSubject,
            HtmlBody = html
        };
    }

    private static string RenderOptionalRichBlock(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return $"""
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;background:#f3f6f9;margin-top:18px;">
                <tr><td style="padding:14px 18px;border-left:3px solid {EmailTemplateDefaults.PrimaryColor};color:#64748b;">{html}</td></tr>
            </table>
            """;
    }

    private static string RenderOptionalContactSection(string heading, string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return $"""
            <p style="margin:22px 0 10px;font-weight:700;color:#243447;">{heading}</p>
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="border-collapse:collapse;background:#f3f6f9;">
                <tr><td style="padding:18px 20px;border-left:3px solid {EmailTemplateDefaults.PrimaryColor};color:#334155;line-height:1.8;">{html}</td></tr>
            </table>
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

        return $"{interviewAt:HH:mm} - {weekday}, ngày {interviewAt:dd/MM/yyyy}";
    }
}
