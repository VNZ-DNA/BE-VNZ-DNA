using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.JobApplicationService;
using VNZ.Service.MailService;
using Xunit;
using JobApplicationService = VNZ.Service.JobApplicationService.Service;

namespace VNZ.Test.JobApplications;

public class ReviewJobApplicationTests
{
    [Fact]
    public async Task ReviewAsync_AcceptsPendingApplicationAndStoresReviewDetails()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var adminId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var cvUrl = "https://files.example/cv.pdf";
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Backend Developer",
            Status = JobPostStatus.Open,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };

        dbContext.Users.Add(new User
        {
            Id = adminId,
            FullName = "Admin Reviewer",
            Email = "admin@vnz.vn",
            PasswordHash = "password-hash",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true
        });
        dbContext.JobPosts.Add(jobPost);
        dbContext.JobApplications.Add(new JobApplication
        {
            Id = applicationId,
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Nguyen Minh Anh",
            Email = "minh.anh@vnz.vn",
            CvUrl = cvUrl,
            Status = JobApplicationStatus.Pending,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();

        var service = new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);

        var response = await service.ReviewAsync(
            applicationId,
            new Request.ReviewJobApplicationRequest { Decision = " Accepted " },
            adminId);

        var savedApplication = await dbContext.JobApplications
            .AsNoTracking()
            .SingleAsync(application => application.Id == applicationId);

        Assert.Equal(applicationId, response.Id);
        Assert.Equal("Đã duyệt", response.Status);
        Assert.Equal("Admin Reviewer", response.ReviewedByName);
        Assert.Equal(cvUrl, response.CvUrl);
        Assert.NotNull(response.ReviewAt);

        Assert.Equal(JobApplicationStatus.Accepted, savedApplication.Status);
        Assert.Equal(adminId, savedApplication.ReviewedBy);
        Assert.Equal(response.ReviewAt, savedApplication.ReviewAt);
        Assert.Equal(response.ReviewAt, savedApplication.UpdateAt);
        Assert.Equal(cvUrl, savedApplication.CvUrl);
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
    }
}
