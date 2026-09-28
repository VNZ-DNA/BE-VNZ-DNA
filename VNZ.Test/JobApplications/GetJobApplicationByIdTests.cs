using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging.Abstractions;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.JobApplicationService;
using VNZ.Service.MailService;
using Xunit;
using JobApplicationService = VNZ.Service.JobApplicationService.Service;

namespace VNZ.Test.JobApplications;

public class GetJobApplicationByIdTests
{
    [Fact]
    public async Task GetJobApplicationByIdAsync_ReturnsApplicantAndSavedJobPostSnapshot()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobApplicationTestDbContext(options);
        var departmentId = Guid.NewGuid();
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Current Backend Developer",
            Status = JobPostStatus.Open,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };
        var applicationId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);
        var snapshot = new JobPostSnapshot
        {
            DepartmentId = departmentId,
            DepartmentName = "Engineering",
            Title = "Backend Developer at application time",
            EmploymentType = EmploymentType.FullTime,
            JobLevel = JobLevel.Junior,
            NumberOfPositions = 2,
            ShortDescription = "Backend role",
            Description = "Job description when the application was submitted",
            Requirements = "C# and PostgreSQL",
            ExpiredAt = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero),
            JobSkillsSnapshot = new JobSkillsSnapshot
            {
                Skills = new List<string> { "C#", "PostgreSQL" }
            }
        };

        dbContext.JobPosts.Add(jobPost);
        dbContext.JobApplications.Add(new JobApplication
        {
            Id = applicationId,
            JobPostId = jobPost.Id,
            JobPost = jobPost,
            FullName = "Nguyen Minh Anh",
            Email = "minh.anh@vnz.vn",
            Phone = "0900000000",
            University = "Dai hoc Bach Khoa",
            Major = "Information Technology",
            GraduationYear = 2025,
            Availability = "Full time",
            AvailableStartDate = "2026-10-01",
            ReferralSource = "Facebook VNZ",
            CvUrl = "https://files.example/cv.pdf",
            PortfolioUrl = "https://portfolio.example",
            CoverLetter = "I am interested in this position.",
            JobPostSnapshot = snapshot,
            Status = JobApplicationStatus.Pending,
            CreatedAt = createdAt
        });
        await dbContext.SaveChangesAsync();

        var service = new JobApplicationService(
            dbContext,
            new StubMailService(),
            NullLogger<JobApplicationService>.Instance);

        var response = await service.GetJobApplicationByIdAsync(applicationId);

        Assert.Equal(applicationId, response.Id);
        Assert.Equal(jobPost.Id, response.JobPostId);
        Assert.Equal("Current Backend Developer", response.JobPostTitle);
        Assert.Equal("Nguyen Minh Anh", response.FullName);
        Assert.Equal("minh.anh@vnz.vn", response.Email);
        Assert.Equal("0900000000", response.Phone);
        Assert.Equal("Dai hoc Bach Khoa", response.University);
        Assert.Equal("Information Technology", response.Major);
        Assert.Equal(2025, response.GraduationYear);
        Assert.Equal("Full time", response.Availability);
        Assert.Equal("2026-10-01", response.AvailableStartDate);
        Assert.Equal("Facebook VNZ", response.ReferralSource);
        Assert.Equal("https://files.example/cv.pdf", response.CvUrl);
        Assert.Equal("https://portfolio.example", response.PortfolioUrl);
        Assert.Equal("I am interested in this position.", response.CoverLetter);
        Assert.Equal(createdAt, response.CreatedAt);

        Assert.NotNull(response.JobPostSnapshot);
        Assert.Equal(departmentId, response.JobPostSnapshot.DepartmentId);
        Assert.Equal("Engineering", response.JobPostSnapshot.DepartmentName);
        Assert.Equal("Backend Developer at application time", response.JobPostSnapshot.Title);
        Assert.Equal(EmploymentType.FullTime, response.JobPostSnapshot.EmploymentType);
        Assert.Equal(JobLevel.Junior, response.JobPostSnapshot.JobLevel);
        Assert.Equal(2, response.JobPostSnapshot.NumberOfPositions);
        Assert.Equal("Backend role", response.JobPostSnapshot.ShortDescription);
        Assert.Equal("Job description when the application was submitted", response.JobPostSnapshot.Description);
        Assert.Equal("C# and PostgreSQL", response.JobPostSnapshot.Requirements);
        Assert.Equal(new[] { "C#", "PostgreSQL" }, response.JobPostSnapshot.JobSkillsSnapshot.Skills);
        Assert.Equal("Chờ duyệt", response.Status);
    }

    private sealed class JobApplicationTestDbContext : AppDbContext
    {
        public JobApplicationTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPost>().Ignore(jobPost => jobPost.Skills);
            modelBuilder.Entity<Product>().Ignore(product => product.Content);

            var snapshotConverter = new ValueConverter<JobPostSnapshot?, string?>(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => JsonSerializer.Deserialize<JobPostSnapshot>(value!, (JsonSerializerOptions?)null));

            modelBuilder.Entity<JobApplication>()
                .Property(application => application.JobPostSnapshot)
                .HasConversion(snapshotConverter);
        }
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
