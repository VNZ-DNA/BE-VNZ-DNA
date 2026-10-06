using System.Text.Json.Serialization;

namespace VNZ.Service.MailService;

public static class Response
{
    public class EmailTemplateBrandingResponse
    {
        public string LogoUrl { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = string.Empty;
        public string FontFamily { get; set; } = string.Empty;
        public int ContentMaxWidthPx { get; set; }
    }

    public class EmailTemplateBlockResponse
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FixedCopyKey { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Required { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Heading { get; set; }
    }

    public class EmailTemplateRichTextPolicyResponse
    {
        public List<string> AllowedTags { get; set; } = new();
        public List<string> AllowedLinkProtocols { get; set; } = new();
        public int MaxHtmlLength { get; set; }
    }

    public class InterviewTemplateFixedCopyResponse
    {
        public string Subject { get; set; } = string.Empty;
        public string Greeting { get; set; } = string.Empty;
        public string Opening { get; set; } = string.Empty;
        public string InterviewInformationHeading { get; set; } = string.Empty;
        public string AgendaHeading { get; set; } = string.Empty;
        public string PreparationHeading { get; set; } = string.Empty;
        public string ConfirmationCopy { get; set; } = string.Empty;
        public string Closing { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }

    public class ContactTemplateFixedCopyResponse
    {
        public string Eyebrow { get; set; } = string.Empty;
        public string Heading { get; set; } = string.Empty;
        public string Greeting { get; set; } = string.Empty;
        public string Opening { get; set; } = string.Empty;
        public string BodyHeading { get; set; } = string.Empty;
        public string ProposalHeading { get; set; } = string.Empty;
        public string NextStepsHeading { get; set; } = string.Empty;
        public string Closing { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
        public string Footer { get; set; } = string.Empty;
        public string FooterTagline { get; set; } = string.Empty;
    }

    public class EmailTemplateDeadlinePolicyResponse
    {
        public string Timezone { get; set; } = string.Empty;
        public int DaysBeforeInterview { get; set; }
        public string Time { get; set; } = string.Empty;
        public string DisplayFormat { get; set; } = string.Empty;
    }

    public class InterviewTemplateSchemaResponse
    {
        public string TemplateKey { get; set; } = string.Empty;
        public EmailTemplateBrandingResponse Branding { get; set; } = new();
        public InterviewTemplateFixedCopyResponse FixedCopy { get; set; } = new();
        public List<EmailTemplateBlockResponse> Blocks { get; set; } = new();
        public EmailTemplateDeadlinePolicyResponse DeadlinePolicy { get; set; } = new();
        public EmailTemplateRichTextPolicyResponse RichTextPolicy { get; set; } = new();
    }

    public class ContactTemplateSchemaResponse
    {
        public string TemplateKey { get; set; } = string.Empty;
        public EmailTemplateBrandingResponse Branding { get; set; } = new();
        public ContactTemplateFixedCopyResponse FixedCopy { get; set; } = new();
        public List<EmailTemplateBlockResponse> Blocks { get; set; } = new();
        public EmailTemplateRichTextPolicyResponse RichTextPolicy { get; set; } = new();
    }
}
