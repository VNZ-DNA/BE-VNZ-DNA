using System.Text.Json;
using VNZ.Service.MailService;
using Xunit;
using RichTextSanitizer = VNZ.Service.Utils.RichTextService.Service;

namespace VNZ.Test.Email;

public sealed class EmailComposerTests
{
    [Fact]
    public void SanitizeEmail_UsesEmailAllowlistAndNormalizesHttpsLinks()
    {
        var sanitizer = new RichTextSanitizer();

        var sanitized = sanitizer.SanitizeEmail(
            "<h2>Ignored heading</h2><p>Hello <strong>team</strong></p>"
            + "<script>alert('x')</script>"
            + "<a href=\"https://example.com/?a=1&amp;b=2\" style=\"color:red\">Open</a>"
            + "<img src=\"https://example.com/image.png\">")!;

        Assert.Contains("<p>Hello <strong>team</strong></p>", sanitized);
        Assert.Contains("<a href=\"https://example.com/?a=1&amp;b=2\">Open</a>", sanitized);
        Assert.DoesNotContain("<h2>", sanitized);
        Assert.DoesNotContain("<script", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<img", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderContact_KeepsFixedLayoutAndHidesEmptyOptionalSections()
    {
        var renderer = new EmailTemplateRenderer();

        var rendered = renderer.RenderContact(new ContactEmailTemplateData
        {
            RecipientName = "<Admin>",
            Subject = "Custom contact subject",
            BodyHtml = "<p>BODY_MARKER <strong>bold</strong></p>"
        });

        Assert.Equal("Custom contact subject", rendered.Subject);
        Assert.Contains("font-size:18px", rendered.HtmlBody);
        Assert.Contains("font-size:14px", rendered.HtmlBody);
        Assert.Contains("BODY_MARKER <strong>bold</strong>", rendered.HtmlBody);
        Assert.Contains("&lt;Admin&gt;", rendered.HtmlBody);
        Assert.DoesNotContain("PROPOSAL_MARKER", rendered.HtmlBody);
        Assert.DoesNotContain("NEXT_STEPS_MARKER", rendered.HtmlBody);
    }

    [Fact]
    public void RenderInterview_UsesDynamicFieldsAndServerDerivedDeadline()
    {
        var renderer = new EmailTemplateRenderer();

        var rendered = renderer.RenderInterview(new InterviewEmailTemplateData
        {
            CandidateName = "Nguyen Van An",
            PositionTitle = "Backend <Engineer>",
            InterviewAt = new DateTimeOffset(2026, 10, 15, 9, 30, 0, TimeSpan.FromHours(7)),
            DurationMinutes = 45,
            InterviewMode = "Online",
            Location = "Office should not be shown for online",
            LocationUrl = "https://meet.example.com/interview",
            InterviewInformationHtml = "<p>INFO_MARKER</p>",
            AgendaHtml = "<ol><li>AGENDA_MARKER</li></ol>",
            PreparationHtml = "<ul><li>PREP_MARKER</li></ul>"
        });

        Assert.Contains("Backend &lt;Engineer&gt;", rendered.HtmlBody);
        Assert.Contains("Thân chào Nguyen Van An,", rendered.HtmlBody);
        Assert.Contains("45 phút", rendered.HtmlBody);
        Assert.Contains("Trực tuyến", rendered.HtmlBody);
        Assert.Contains("INFO_MARKER", rendered.HtmlBody);
        Assert.Contains("AGENDA_MARKER", rendered.HtmlBody);
        Assert.Contains("PREP_MARKER", rendered.HtmlBody);
        Assert.Contains("14:00 ngày 13/10/2026", rendered.HtmlBody);
        Assert.DoesNotContain("Office should not be shown for online", rendered.HtmlBody);
    }

    [Fact]
    public void RenderJobApplicationReceived_UsesSavedValuesAndVietnamTime()
    {
        var renderer = new EmailTemplateRenderer();

        var rendered = renderer.RenderJobApplicationReceived(new JobApplicationReceivedEmailTemplateData
        {
            CandidateName = "<Nguyen Minh Anh>",
            PositionTitle = "Backend <Engineer>",
            ReceivedAt = new DateTimeOffset(2026, 10, 1, 2, 30, 0, TimeSpan.Zero)
        });

        Assert.Equal("VNZ Technology đã nhận được hồ sơ ứng tuyển của bạn", rendered.Subject);
        Assert.Contains("&lt;Nguyen Minh Anh&gt;", rendered.HtmlBody);
        Assert.Contains("Backend &lt;Engineer&gt;", rendered.HtmlBody);
        Assert.Contains("01/10/2026 09:30", rendered.HtmlBody);
        Assert.Contains("THÔNG BÁO TIẾP NHẬN HỒ SƠ", rendered.HtmlBody);
        Assert.DoesNotContain("<Nguyen Minh Anh>", rendered.HtmlBody);
    }

    [Fact]
    public void InterviewTemplateSchema_ReturnsFixedCopyBlocksAndDeadlinePolicy()
    {
        var schema = EmailTemplateSchemaProvider.GetInterviewSchema();
        var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.Equal("interview-invitation", schema.TemplateKey);
        Assert.Equal("Thân chào {{candidate.fullName}},", schema.FixedCopy.Greeting);
        Assert.Equal("14:00", schema.DeadlinePolicy.Time);
        Assert.Equal("HH:mm 'ngày' dd/MM/yyyy", schema.DeadlinePolicy.DisplayFormat);
        Assert.Equal(2, schema.DeadlinePolicy.DaysBeforeInterview);
        Assert.Contains("{{deadline}}", schema.FixedCopy.ConfirmationCopy);
        Assert.Equal("greeting", schema.Blocks.Single(block => block.Key == "greeting").FixedCopyKey);
        Assert.DoesNotContain("templateVersion", schemaJson, StringComparison.Ordinal);
        Assert.Contains("deadlinePolicy", schemaJson, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactTemplateSchema_UsesFixedCopyKeyAndOmitsDeadlinePolicy()
    {
        var schema = EmailTemplateSchemaProvider.GetContactSchema();
        var schemaJson = JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.Equal("contact-reply", schema.TemplateKey);
        Assert.Equal("greeting", schema.Blocks.Single(block => block.Key == "greeting").FixedCopyKey);
        Assert.Equal("bodyHeading", schema.Blocks.Single(block => block.Key == "body").FixedCopyKey);
        Assert.Equal("proposalHeading", schema.Blocks.Single(block => block.Key == "proposalHtml").FixedCopyKey);
        Assert.Equal("nextStepsHeading", schema.Blocks.Single(block => block.Key == "nextStepsHtml").FixedCopyKey);
        Assert.Equal("VNZ Technology", schema.FixedCopy.Footer);
        Assert.Equal("Vietnamese Minds • Global Solutions", schema.FixedCopy.FooterTagline);
        var footerBlock = schema.Blocks.Single(block => block.Key == "footer");
        Assert.Equal("footer", footerBlock.Type);
        Assert.Null(footerBlock.FixedCopyKey);
        Assert.Null(schema.Blocks.Single(block => block.Key == "body").Heading);
        Assert.Contains("footerTagline", schemaJson, StringComparison.Ordinal);
        Assert.DoesNotContain("deadlinePolicy", schemaJson, StringComparison.Ordinal);
        Assert.DoesNotContain("templateVersion", schemaJson, StringComparison.Ordinal);
    }

}
