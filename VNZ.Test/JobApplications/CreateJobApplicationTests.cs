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

public class CreateJobApplicationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_RejectsJobPostWithoutTitleBeforeSavingOrSendingEmail(string? title)
    {
        await using var dbContext = CreateDbContext();
        var jobPost = await AddOpenJobPostAsync(dbContext);
        jobPost.Title = title;
        await dbContext.SaveChangesAsync();
        var mailService = new StubMailService();
        var service = new JobApplicationService(
            dbContext,
            mailService,
            NullLogger<JobApplicationService>.Instance);

        var exception = await Assert.ThrowsAsync<JobApplicationException>(() =>
            service.CreateAsync(CreateValidRequest(jobPost.Id)));

        Assert.Equal("JOB_APPLICATION_CREATE_FAILED", exception.Code);
        Assert.Empty(await dbContext.JobApplications.ToListAsync());
        Assert.Null(mailService.SentMail);
    }

    [Fact]
    public async Task CreateAsync_SavesApplicationThenSendsConfirmationEmail()
    {
        await using var dbContext = CreateDbContext();
        var jobPost = await AddOpenJobPostAsync(dbContext);
        var mailService = new StubMailService();
        var service = new JobApplicationService(
            dbContext,
            mailService,
            NullLogger<JobApplicationService>.Instance);

        var response = await service.CreateAsync(CreateValidRequest(jobPost.Id));

        var savedApplication = await dbContext.JobApplications
            .AsNoTracking()
            .SingleAsync(application => application.Id == response.Id);

        Assert.Equal(JobApplicationStatus.Pending, savedApplication.Status);
        Assert.NotNull(mailService.SentMail);
        Assert.Equal(savedApplication.Email, mailService.SentMail!.To);
        Assert.Equal(savedApplication.FullName, mailService.SentMail.ToName);
        Assert.Equal("VNZ Technology đã nhận được hồ sơ ứng tuyển của bạn", mailService.SentMail.Subject);
        Assert.Equal($"job-application-received-{savedApplication.Id}", mailService.SentMail.IdempotencyKey);
        Assert.Equal("job-application-received", mailService.SentMail.Tag);
        Assert.True(mailService.SentMail.IsHtmlBody);
        Assert.Contains("Backend Developer", mailService.SentMail.Body);
    }

    [Fact]
    public async Task CreateAsync_KeepsSavedApplicationWhenConfirmationEmailFails()
    {
        await using var dbContext = CreateDbContext();
        var jobPost = await AddOpenJobPostAsync(dbContext);
        var mailService = new StubMailService { ThrowWhenSending = true };
        var service = new JobApplicationService(
            dbContext,
            mailService,
            NullLogger<JobApplicationService>.Instance);

        var response = await service.CreateAsync(CreateValidRequest(jobPost.Id));

        var savedApplication = await dbContext.JobApplications
            .AsNoTracking()
            .SingleAsync(application => application.Id == response.Id);

        Assert.Equal(JobApplicationStatus.Pending, savedApplication.Status);
        Assert.NotNull(mailService.SentMail);
        Assert.Equal(1, await dbContext.JobApplications.CountAsync());
    }

    private static VNZ.Test.TeamMembers.TestAppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VNZ.Test.TeamMembers.TestAppDbContext(options);
    }

    private static async Task<JobPost> AddOpenJobPostAsync(AppDbContext dbContext)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "ENGINEERING",
            Name = "Engineering",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Department = department,
            Title = "Backend Developer",
            Status = JobPostStatus.Open,
            ExpiredAt = DateTimeOffset.UtcNow.AddDays(1),
            CreatedAt = DateTimeOffset.UtcNow,
            EmploymentType = EmploymentType.Internship,
            JobLevel = JobLevel.Intern,
            NumberOfPositions = 1
        };

        dbContext.Departments.Add(department);
        dbContext.JobPosts.Add(jobPost);
        await dbContext.SaveChangesAsync();

        return jobPost;
    }

    private static Request.CreateJobApplicationRequest CreateValidRequest(Guid jobPostId)
    {
        return new Request.CreateJobApplicationRequest
        {
            JobPostId = jobPostId,
            FullName = "Nguyen Minh Anh",
            Email = "MINH.ANH@EXAMPLE.COM",
            Phone = "0901234567",
            CvUrl = "https://files.example/cv.pdf",
            CoverLetter = "I would like to apply for this position.",
            ConsentToDataProcessing = true
        };
    }

    private sealed class StubMailService : VNZ.Service.MailService.IService
    {
        public MailContent? SentMail { get; private set; }
        public bool ThrowWhenSending { get; set; }

        public Task SendAsync(MailContent content)
        {
            SentMail = content;

            if (ThrowWhenSending)
            {
                throw new HttpRequestException("Mail provider is unavailable.");
            }

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
