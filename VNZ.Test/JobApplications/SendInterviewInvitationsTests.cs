using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using VNZ.Service.JobApplicationService;
using VNZ.Service.MailService;
using Xunit;
using JobApplicationService = VNZ.Service.JobApplicationService.Service;

namespace VNZ.Test.JobApplications;

public class SendInterviewInvitationsTests
{
    [Fact]
    public async Task SendInterviewInvitationsAsync_RechecksApplicationStatusBeforeSending()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Backend Developer",
            Status = JobPostStatus.Closed,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Pending Applicant",
            Email = "pending@example.com",
            Status = JobApplicationStatus.Pending,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        };

        dbContext.JobPosts.Add(jobPost);
        dbContext.JobApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var service = new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);

        var interviewAt = DateTimeOffset.UtcNow.AddDays(1).ToOffset(TimeSpan.FromHours(7));
        var exception = await Assert.ThrowsAsync<JobApplicationException>(() =>
            service.SendInterviewInvitationsAsync(new Request.SendInterviewInvitationsRequest
            {
                ApplicationIds = new List<Guid> { application.Id },
                InterviewDate = interviewAt.ToString("yyyy-MM-dd"),
                InterviewTime = interviewAt.ToString("HH:mm"),
                DurationMinutes = 60,
                InterviewMode = "Online",
                LocationUrl = "https://meet.example.com/interview",
                AgendaHtml = "<p>Agenda</p>",
                PreparationHtml = "<p>Preparation</p>"
            }));

        Assert.Equal("JOB_APPLICATION_INTERVIEW_BATCH_INVALID", exception.Code);
    }

    private sealed class StubMailService : VNZ.Service.MailService.IService
    {
        public Task SendAsync(MailContent content)
        {
            return Task.CompletedTask;
        }

        public Task<MailDeliveryResult> SendInterviewInvitationAsync(
            InterviewInvitationMailContent content)
        {
            return Task.FromResult(new MailDeliveryResult { IsSuccess = true });
        }

        public Task<MailDeliveryResult> SendRejectionEmailAsync(
            RejectionEmailMailContent content)
        {
            return Task.FromResult(new MailDeliveryResult { IsSuccess = true });
        }
    }
}
