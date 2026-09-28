using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.JobPostService;
using Xunit;
using JobPostService = VNZ.Service.JobPostService.Service;

namespace VNZ.Test.JobPosts;

public class GetPublicJobPostListTests
{
    [Fact]
    public async Task GetPublicJobPostListAsync_ReturnsOnlyOpenUnexpiredPostsInExpiryOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PublicJobPostTestDbContext(options);
        var engineering = new Department
        {
            Id = Guid.NewGuid(),
            Name = "Kỹ thuật",
            CreatedAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
        };

        var expiresFirst = CreateVietnamExpiry(new DateOnly(2030, 10, 31));
        var expiresSecond = CreateVietnamExpiry(new DateOnly(2030, 11, 30));
        var firstJobPost = CreateJobPost(
            "Thực tập sinh Backend (.NET)",
            JobPostStatus.Open,
            expiresFirst,
            new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero));
        firstJobPost.DepartmentId = engineering.Id;
        firstJobPost.Department = engineering;
        firstJobPost.EmploymentType = EmploymentType.Internship;
        firstJobPost.JobLevel = JobLevel.Intern;
        firstJobPost.NumberOfPositions = 2;
        firstJobPost.Skills = new List<string> { "C#", ".NET" };
        firstJobPost.ShortDescription = "Tham gia phát triển API và dịch vụ nội bộ.";

        var secondJobPost = CreateJobPost(
            "Chuyên viên Nhân sự",
            JobPostStatus.Open,
            expiresSecond,
            new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero));
        secondJobPost.EmploymentType = EmploymentType.FullTime;
        secondJobPost.JobLevel = JobLevel.Junior;
        secondJobPost.NumberOfPositions = 1;
        secondJobPost.Skills = new List<string>();

        dbContext.Departments.Add(engineering);
        dbContext.JobPosts.AddRange(
            secondJobPost,
            CreateJobPost(
                "Draft job",
                JobPostStatus.Draft,
                expiresFirst,
                new DateTimeOffset(2026, 9, 22, 8, 0, 0, TimeSpan.Zero)),
            CreateJobPost(
                "Closed job",
                JobPostStatus.Closed,
                expiresFirst,
                new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero)),
            CreateJobPost(
                "Expired status job",
                JobPostStatus.Expired,
                expiresFirst,
                new DateTimeOffset(2026, 9, 23, 8, 0, 0, TimeSpan.Zero)),
            CreateJobPost(
                "Past expiry job",
                JobPostStatus.Open,
                CreateVietnamExpiry(new DateOnly(2025, 10, 31)),
                new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero)),
            CreateJobPost(
                "No expiry job",
                JobPostStatus.Open,
                null,
                new DateTimeOffset(2026, 9, 25, 8, 0, 0, TimeSpan.Zero)),
            firstJobPost);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);

        var response = await service.GetPublicJobPostListAsync();

        Assert.Equal(2, response.Count);

        Assert.Equal(firstJobPost.Id, response[0].Id);
        Assert.Equal("Thực tập sinh Backend (.NET)", response[0].Title);
        Assert.Equal("Kỹ thuật", response[0].Department);
        Assert.Equal("Thực tập", response[0].EmploymentType);
        Assert.Equal("Intern", response[0].JobLevel);
        Assert.Equal(2, response[0].NumberOfPositions);
        Assert.Equal(new[] { "C#", ".NET" }, response[0].Skills);
        Assert.Equal("Tham gia phát triển API và dịch vụ nội bộ.", response[0].ShortDescription);
        Assert.Equal(new DateOnly(2030, 10, 31), response[0].ExpiredDate);

        Assert.Equal(secondJobPost.Id, response[1].Id);
        Assert.Equal("Chuyên viên Nhân sự", response[1].Title);
        Assert.Null(response[1].Department);
        Assert.Equal("Toàn thời gian", response[1].EmploymentType);
        Assert.Equal("Junior", response[1].JobLevel);
        Assert.Equal(1, response[1].NumberOfPositions);
        Assert.Empty(response[1].Skills);
        Assert.Null(response[1].ShortDescription);
        Assert.Equal(new DateOnly(2030, 11, 30), response[1].ExpiredDate);
    }

    private static JobPost CreateJobPost(
        string title,
        JobPostStatus status,
        DateTimeOffset? expiredAt,
        DateTimeOffset createdAt)
    {
        return new JobPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = status,
            ExpiredAt = expiredAt,
            CreatedAt = createdAt,
            Skills = new List<string>()
        };
    }

    private static DateTimeOffset CreateVietnamExpiry(DateOnly finalDay)
    {
        var nextDayAtMidnightInVietnam = new DateTimeOffset(
            finalDay.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(7));

        return nextDayAtMidnightInVietnam.ToUniversalTime();
    }

    private sealed class PublicJobPostTestDbContext : AppDbContext
    {
        public PublicJobPostTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Content);

            var skillsConverter = new ValueConverter<List<string>, string>(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!);

            modelBuilder.Entity<JobPost>()
                .Property(jobPost => jobPost.Skills)
                .HasConversion(skillsConverter);
        }
    }
}
