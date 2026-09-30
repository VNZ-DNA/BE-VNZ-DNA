namespace VNZ.Service.MailService;

public static class EmailTemplateSchemaProvider
{
    public static Response.InterviewTemplateSchemaResponse GetInterviewSchema()
    {
        return new Response.InterviewTemplateSchemaResponse
        {
            TemplateKey = "interview-invitation",
            Branding = CreateBranding(),
            FixedCopy = new Response.InterviewTemplateFixedCopyResponse
            {
                Subject = EmailTemplateDefaults.InterviewSubject,
                Greeting = EmailTemplateDefaults.InterviewGreetingTemplate,
                Opening = EmailTemplateDefaults.InterviewOpeningTemplate,
                InterviewInformationHeading = EmailTemplateDefaults.InterviewInformationHeading,
                AgendaHeading = EmailTemplateDefaults.InterviewAgendaHeading,
                PreparationHeading = EmailTemplateDefaults.InterviewPreparationHeading,
                ConfirmationCopy = EmailTemplateDefaults.InterviewConfirmationTemplate,
                Closing = EmailTemplateDefaults.InterviewClosing,
                Signature = EmailTemplateDefaults.InterviewSignature
            },
            Blocks = new List<Response.EmailTemplateBlockResponse>
            {
                CreateFixedTextBlock("logo", "logo"),
                CreateFixedTextBlock("greeting", "text", "greeting"),
                CreateFixedTextBlock("opening", "text", "opening"),
                new Response.EmailTemplateBlockResponse
                {
                    Key = "interviewInfo",
                    Type = "interview-info"
                },
                new Response.EmailTemplateBlockResponse
                {
                    Key = "interviewInformationHtml",
                    Type = "rich-text",
                    Required = false
                },
                new Response.EmailTemplateBlockResponse
                {
                    Key = "agendaHtml",
                    Type = "rich-text",
                    Heading = EmailTemplateDefaults.InterviewAgendaHeading,
                    Required = true
                },
                new Response.EmailTemplateBlockResponse
                {
                    Key = "preparationHtml",
                    Type = "rich-text",
                    Heading = EmailTemplateDefaults.InterviewPreparationHeading,
                    Required = true
                },
                CreateFixedTextBlock("confirmation", "text", "confirmationCopy"),
                CreateFixedTextBlock("closing", "text", "closing"),
                CreateFixedTextBlock("signature", "text", "signature")
            },
            DeadlinePolicy = new Response.EmailTemplateDeadlinePolicyResponse
            {
                Timezone = "Asia/Ho_Chi_Minh",
                DaysBeforeInterview = 2,
                Time = "14:00"
            },
            RichTextPolicy = CreateRichTextPolicy()
        };
    }

    public static Response.ContactTemplateSchemaResponse GetContactSchema()
    {
        return new Response.ContactTemplateSchemaResponse
        {
            TemplateKey = "contact-reply",
            Branding = CreateBranding(),
            FixedCopy = new Response.ContactTemplateFixedCopyResponse
            {
                Eyebrow = EmailTemplateDefaults.ContactEyebrow,
                Heading = EmailTemplateDefaults.ContactHeading,
                Greeting = EmailTemplateDefaults.ContactGreetingTemplate,
                Opening = EmailTemplateDefaults.ContactOpening,
                BodyHeading = EmailTemplateDefaults.ContactBodyHeading,
                ProposalHeading = EmailTemplateDefaults.ContactProposalHeading,
                NextStepsHeading = EmailTemplateDefaults.ContactNextStepsHeading,
                Closing = EmailTemplateDefaults.ContactClosing,
                Signature = EmailTemplateDefaults.ContactSignature,
                Footer = EmailTemplateDefaults.ContactFooter
            },
            Blocks = new List<Response.EmailTemplateBlockResponse>
            {
                CreateFixedTextBlock("logo", "logo"),
                CreateFixedTextBlock("eyebrow", "text", "eyebrow"),
                CreateFixedTextBlock("heading", "text", "heading"),
                CreateFixedTextBlock("greeting", "text", "greeting"),
                CreateFixedTextBlock("opening", "text", "opening"),
                new Response.EmailTemplateBlockResponse
                {
                    Key = "body",
                    Type = "rich-text",
                    Heading = EmailTemplateDefaults.ContactBodyHeading,
                    Required = true
                },
                new Response.EmailTemplateBlockResponse
                {
                    Key = "proposalHtml",
                    Type = "rich-text",
                    Heading = EmailTemplateDefaults.ContactProposalHeading,
                    Required = false
                },
                new Response.EmailTemplateBlockResponse
                {
                    Key = "nextStepsHtml",
                    Type = "rich-text",
                    Heading = EmailTemplateDefaults.ContactNextStepsHeading,
                    Required = false
                },
                CreateFixedTextBlock("closing", "text", "closing"),
                CreateFixedTextBlock("signature", "text", "signature"),
                CreateFixedTextBlock("footer", "text", "footer")
            },
            RichTextPolicy = CreateRichTextPolicy()
        };
    }

    private static Response.EmailTemplateBrandingResponse CreateBranding()
    {
        return new Response.EmailTemplateBrandingResponse
        {
            LogoUrl = EmailTemplateDefaults.VnzLogoUrl,
            PrimaryColor = EmailTemplateDefaults.PrimaryColor,
            FontFamily = EmailTemplateDefaults.FontFamily,
            ContentMaxWidthPx = EmailTemplateDefaults.ContentMaxWidthPx
        };
    }

    private static Response.EmailTemplateRichTextPolicyResponse CreateRichTextPolicy()
    {
        return new Response.EmailTemplateRichTextPolicyResponse
        {
            AllowedTags = new List<string>
            {
                "p", "br", "strong", "em", "u", "s", "ul", "ol", "li", "blockquote", "a"
            },
            AllowedLinkProtocols = new List<string> { "https" },
            MaxHtmlLength = 20_000
        };
    }

    private static Response.EmailTemplateBlockResponse CreateFixedTextBlock(
        string key,
        string type,
        string? fixedCopyKey = null)
    {
        return new Response.EmailTemplateBlockResponse
        {
            Key = key,
            Type = type,
            FixedCopyKey = fixedCopyKey
        };
    }
}
