using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.JobApplicationService;
using VNZ.Service.MailService;
using Xunit;
using JobApplicationService = VNZ.Service.JobApplicationService.Service;

namespace VNZ.Test.JobApplications;

public class GetJobApplicationFilterTests
{
    [Fact]
    public async Task GetJobApplicationListAsync_AppliesSearchMultiStatusAndMultiPositionFilters()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var backendJobPost = CreateJobPost("Backend Developer", JobPostStatus.Open);
        var frontendJobPost = CreateJobPost("Frontend Developer", JobPostStatus.Open);
        var closedJobPost = CreateJobPost("Minh Position", JobPostStatus.Closed);

        var matchingName = CreateApplication(
            backendJobPost,
            "Minh Applicant",
            "applicant@example.com",
            JobApplicationStatus.Accepted);
        var matchingEmail = CreateApplication(
            frontendJobPost,
            "Nguyen Applicant",
            "minh@example.com",
            JobApplicationStatus.Pending);
        var positionOnlyMatch = CreateApplication(
            closedJobPost,
            "Lan Applicant",
            "lan@example.com",
            JobApplicationStatus.Pending);

        dbContext.JobPosts.AddRange(backendJobPost, frontendJobPost, closedJobPost);
        dbContext.JobApplications.AddRange(matchingName, matchingEmail, positionOnlyMatch);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetJobApplicationListAsync(
            new Request.GetJobApplicationListRequest
            {
                Search = "Minh",
                Status = new List<string> { "Accepted", "Pending" },
                JobPostId = new List<Guid>
                {
                    backendJobPost.Id,
                    frontendJobPost.Id,
                    closedJobPost.Id
                },
                PageSize = 10
            });

        Assert.Equal(2, response.Total);
        Assert.Equal(10, response.PageSize);
        Assert.Contains(response.Items, item => item.Id == matchingName.Id);
        Assert.Contains(response.Items, item => item.Id == matchingEmail.Id);
        Assert.DoesNotContain(response.Items, item => item.Id == positionOnlyMatch.Id);
    }

    [Fact]
    public async Task GetJobApplicationFilterOptionsAsync_ReturnsPostsWithManagedApplicationsRegardlessOfPostStatus()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var closedJobPost = CreateJobPost("A Closed Position", JobPostStatus.Closed);
        var openJobPost = CreateJobPost("B Open Position", JobPostStatus.Open);
        var expiredJobPost = CreateJobPost("C Expired Position", JobPostStatus.Open);
        expiredJobPost.ExpiredAt = DateTimeOffset.UtcNow.AddDays(-1);
        var jobPostWithoutApplications = CreateJobPost("D Empty Position", JobPostStatus.Open);
        var jobPostWithDeletedApplication = CreateJobPost("E Deleted Application Position", JobPostStatus.Open);

        dbContext.JobPosts.AddRange(
            closedJobPost,
            openJobPost,
            expiredJobPost,
            jobPostWithoutApplications,
            jobPostWithDeletedApplication);
        var deletedApplication = CreateApplication(
            jobPostWithDeletedApplication,
            "Deleted Applicant",
            "deleted@example.com",
            JobApplicationStatus.Pending);
        deletedApplication.IsDelete = true;
        dbContext.JobApplications.AddRange(
            CreateApplication(closedJobPost, "Closed Applicant", "closed@example.com", JobApplicationStatus.Rejected),
            CreateApplication(openJobPost, "Open Applicant", "open@example.com", JobApplicationStatus.Pending),
            CreateApplication(expiredJobPost, "Expired Applicant", "expired@example.com", JobApplicationStatus.Pending),
            deletedApplication);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetJobApplicationFilterOptionsAsync();

        Assert.Equal(3, response.JobPosts.Count);
        Assert.Contains(response.JobPosts, option => option.Id == closedJobPost.Id);
        Assert.Contains(response.JobPosts, option => option.Id == openJobPost.Id);
        Assert.Contains(response.JobPosts, option => option.Id == expiredJobPost.Id);
        Assert.DoesNotContain(response.JobPosts, option => option.Id == jobPostWithoutApplications.Id);
        Assert.DoesNotContain(response.JobPosts, option => option.Id == jobPostWithDeletedApplication.Id);
    }

    [Fact]
    public async Task GetJobApplicationListAsync_RejectsPageSizeOutsideFrontendOptions()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetJobApplicationListAsync(
            new Request.GetJobApplicationListRequest { PageSize = 1 }));
    }

    private static JobApplicationService CreateService(AppDbContext dbContext)
    {
        return new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);
    }

    private static JobPost CreateJobPost(string title, JobPostStatus status)
    {
        return new JobPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = status,
            ExpiredAt = status == JobPostStatus.Open
                ? DateTimeOffset.UtcNow.AddDays(30)
                : null,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };
    }

    private static JobApplication CreateApplication(
        JobPost jobPost,
        string fullName,
        string email,
        JobApplicationStatus status)
    {
        return new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = fullName,
            Email = email,
            CvUrl = "https://files.example/cv.pdf",
            Status = status,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        };
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
