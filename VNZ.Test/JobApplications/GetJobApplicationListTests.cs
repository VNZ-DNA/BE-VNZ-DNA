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

public class GetJobApplicationListTests
{
    [Fact]
    public async Task GetJobApplicationListAsync_ReturnsApplicationsInExpectedOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Backend Developer",
            Status = JobPostStatus.Open,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var pendingApplication = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Nguyen Minh Anh",
            Email = "minh.anh@vnz.vn",
            CvUrl = "pending-cv.pdf",
            Status = JobApplicationStatus.Pending,
            CreatedAt = new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero)
        };

        var acceptedApplication = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Tran Gia Han",
            Email = "gia.han@vnz.vn",
            CvUrl = "accepted-cv.pdf",
            Status = JobApplicationStatus.Accepted,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        };

        dbContext.JobPosts.Add(jobPost);
        dbContext.JobApplications.AddRange(pendingApplication, acceptedApplication);
        await dbContext.SaveChangesAsync();

        var service = new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);

        var response = await service.GetJobApplicationListAsync(
            new Request.GetJobApplicationListRequest());

        Assert.Equal(2, response.Total);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(2, response.Items.Count);

        Assert.Equal(acceptedApplication.Id, response.Items[0].Id);
        Assert.Equal("Backend Developer", response.Items[0].JobPostTitle);
        Assert.Equal("Đã duyệt", response.Items[0].Status);
        Assert.True(response.Items[0].CanSelectForInterviewEmail);

        Assert.Equal(pendingApplication.Id, response.Items[1].Id);
        Assert.Equal("Chờ duyệt", response.Items[1].Status);
        Assert.False(response.Items[1].CanSelectForInterviewEmail);
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
