using VNZ.Service.MailService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using Xunit;
using RichTextSanitizer = VNZ.Service.Utils.RichTextService.Service;
using ContactService = VNZ.Service.ContactService.Service;
using JobApplicationService = VNZ.Service.JobApplicationService.Service;

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
        Assert.Contains("45 phút", rendered.HtmlBody);
        Assert.Contains("Trực tuyến", rendered.HtmlBody);
        Assert.Contains("INFO_MARKER", rendered.HtmlBody);
        Assert.Contains("AGENDA_MARKER", rendered.HtmlBody);
        Assert.Contains("PREP_MARKER", rendered.HtmlBody);
        Assert.Contains("13/10/2026", rendered.HtmlBody);
        Assert.Contains("14:00", rendered.HtmlBody);
        Assert.DoesNotContain("Office should not be shown for online", rendered.HtmlBody);
    }

    [Fact]
    public async Task InterviewPreview_DoesNotChangeApplicationState()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Backend Engineer",
            Status = JobPostStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Nguyen Van An",
            Email = "an@example.com",
            CvUrl = "https://files.example/cv.pdf",
            Status = JobApplicationStatus.Accepted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdateAt = DateTimeOffset.UtcNow
        };

        dbContext.JobPosts.Add(jobPost);
        dbContext.JobApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);

        var result = await service.PreviewInterviewInvitationAsync(new VNZ.Service.JobApplicationService.Request.SendInterviewInvitationsRequest
        {
            ApplicationIds = new List<Guid> { application.Id },
            PreviewApplicationId = application.Id,
            InterviewDate = "2099-10-15",
            InterviewTime = "09:30",
            DurationMinutes = 30,
            InterviewMode = "Onsite",
            Location = "84 đường D5",
            LocationUrl = "https://maps.example.com/interview",
            AgendaHtml = "<p>Agenda</p>",
            PreparationHtml = "<p>Preparation</p>"
        });

        var savedApplication = await dbContext.JobApplications
            .AsNoTracking()
            .SingleAsync(item => item.Id == application.Id);

        Assert.Equal(application.Id, result.ApplicationId);
        Assert.Contains("Backend Engineer", result.Html);
        Assert.Equal(JobApplicationStatus.Accepted, savedApplication.Status);
        Assert.Null(savedApplication.InterViewAt);
    }

    [Fact]
    public async Task ContactPreview_DoesNotMarkContactAsReadOrContacted()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var contact = new ContactInquiry
        {
            Id = Guid.NewGuid(),
            InquiryTopic = ContactInquiryTopic.Other,
            FullName = "Nguyen Minh Long",
            Email = "long@example.com",
            Message = "Need a website",
            ConsentToDataProcessing = true,
            IsRead = false,
            ContactStatus = ContactStatus.NotContacted,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ContactInquiries.Add(contact);
        await dbContext.SaveChangesAsync();

        var service = new ContactService(
            dbContext,
            new StubMailService(),
            new HttpContextAccessor());

        var result = await service.PreviewReplyAsync(contact.Id, new VNZ.Service.ContactService.Request.SendContactReplyRequest
        {
            Subject = "Custom subject",
            Body = "<p>BODY_MARKER</p>",
            ProposalHtml = "<ul><li>PROPOSAL_MARKER</li></ul>",
            NextStepsHtml = "<p>NEXT_STEPS_MARKER</p>"
        });

        var savedContact = await dbContext.ContactInquiries
            .AsNoTracking()
            .SingleAsync(item => item.Id == contact.Id);

        Assert.Equal(contact.Id, result.ContactId);
        Assert.Contains("BODY_MARKER", result.Html);
        Assert.Contains("PROPOSAL_MARKER", result.Html);
        Assert.Contains("NEXT_STEPS_MARKER", result.Html);
        Assert.False(savedContact.IsRead);
        Assert.Equal(ContactStatus.NotContacted, savedContact.ContactStatus);
    }

    private sealed class StubMailService : IService
    {
        public Task SendAsync(MailContent content) => Task.CompletedTask;

        public Task<MailDeliveryResult> SendInterviewInvitationAsync(InterviewInvitationMailContent content)
        {
            return Task.FromResult(new MailDeliveryResult { IsSuccess = true });
        }

        public Task<MailDeliveryResult> SendRejectionEmailAsync(RejectionEmailMailContent content)
        {
            return Task.FromResult(new MailDeliveryResult { IsSuccess = true });
        }
    }
}
