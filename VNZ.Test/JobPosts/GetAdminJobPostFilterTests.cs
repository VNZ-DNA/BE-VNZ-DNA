using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using VNZ.Service.JobPostService;
using Xunit;
using JobPostService = VNZ.Service.JobPostService.Service;

namespace VNZ.Test.JobPosts;

public class GetAdminJobPostFilterTests
{
    [Fact]
    public async Task GetJobPostListAsync_AppliesMultiSelectFiltersWithOrWithinEachGroup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);

        var departmentA = new Department { Id = Guid.NewGuid(), Name = "Kỹ thuật", CreatedAt = DateTimeOffset.UtcNow };
        var departmentB = new Department { Id = Guid.NewGuid(), Name = "Sản phẩm", CreatedAt = DateTimeOffset.UtcNow };

        var expectedOpen = CreateJobPost("Backend A", JobPostStatus.Open, departmentA.Id, JobLevel.Junior, 3);
        var expectedClosed = CreateJobPost("Backend B", JobPostStatus.Closed, departmentB.Id, JobLevel.Senior, 2);
        var wrongDepartment = CreateJobPost("Backend C", JobPostStatus.Open, Guid.NewGuid(), JobLevel.Junior, 1);
        var wrongLevel = CreateJobPost("Backend Lead", JobPostStatus.Open, departmentA.Id, JobLevel.Lead, 4);
        var deleted = CreateJobPost("Backend deleted", JobPostStatus.Open, departmentA.Id, JobLevel.Junior, 5);
        deleted.IsDelete = true;

        dbContext.Departments.AddRange(departmentA, departmentB);
        dbContext.JobPosts.AddRange(expectedOpen, expectedClosed, wrongDepartment, wrongLevel, deleted);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);

        var response = await service.GetJobPostListAsync(new Request.GetJobPostListRequest
        {
            Search = " backend ",
            Status = [nameof(JobPostStatus.Open), nameof(JobPostStatus.Closed)],
            DepartmentId = [departmentA.Id.ToString(), departmentB.Id.ToString()],
            JobLevel = [nameof(JobLevel.Junior), nameof(JobLevel.Senior)],
            PageSize = 15
        });

        Assert.Equal(15, response.PageSize);
        Assert.Equal(2, response.Total);
        Assert.Equal(2, response.Items.Count);
        Assert.Contains(response.Items, item => item.Id == expectedOpen.Id);
        Assert.Contains(response.Items, item => item.Id == expectedClosed.Id);
        Assert.DoesNotContain(response.Items, item => item.Id == deleted.Id);
    }

    [Theory]
    [InlineData("Published", "JOB_POST_INVALID_STATUS_FILTER", "status")]
    [InlineData("Unknown", "JOB_POST_INVALID_JOB_LEVEL_FILTER", "jobLevel")]
    public async Task GetJobPostListAsync_RejectsInvalidEnumFilter(string value, string code, string field)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var service = new JobPostService(dbContext);
        var request = new Request.GetJobPostListRequest();

        if (field == "status")
        {
            request.Status = [value];
        }
        else
        {
            request.JobLevel = [value];
        }

        var exception = await Assert.ThrowsAsync<JobPostException>(() => service.GetJobPostListAsync(request));

        Assert.Equal(code, exception.Code);
        Assert.Equal([field], exception.Fields);
    }

    [Fact]
    public async Task GetJobPostListAsync_RejectsInvalidDepartmentId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var service = new JobPostService(dbContext);

        var exception = await Assert.ThrowsAsync<JobPostException>(() => service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { DepartmentId = ["not-a-guid"] }));

        Assert.Equal("JOB_POST_INVALID_DEPARTMENT_FILTER", exception.Code);
        Assert.Equal(["departmentId"], exception.Fields);
    }

    [Fact]
    public async Task GetJobPostListAsync_FiltersByExactExpiredDate()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var expectedJobPost = CreateJobPost(
            "Expected job",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            1);
        expectedJobPost.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));

        var wrongJobPost = CreateJobPost(
            "Wrong job",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            2);
        wrongJobPost.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 11, 30));

        dbContext.JobPosts.AddRange(expectedJobPost, wrongJobPost);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);

        var response = await service.GetJobPostListAsync(new Request.GetJobPostListRequest
        {
            ExpiredDate = "2030-10-31"
        });

        Assert.Equal(1, response.Total);
        Assert.Single(response.Items);
        Assert.Equal(expectedJobPost.Id, response.Items[0].Id);
        Assert.Equal(new DateOnly(2030, 10, 31), response.Items[0].ExpiredDate);
    }

    [Fact]
    public async Task GetJobPostListAsync_SortsByExpiredDateAndPlacesMissingDatesLast()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var earliest = CreateJobPost(
            "Earliest",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            1);
        earliest.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));

        var latest = CreateJobPost(
            "Latest",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            2);
        latest.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 11, 30));

        var withoutExpiry = CreateJobPost(
            "Without expiry",
            JobPostStatus.Draft,
            Guid.NewGuid(),
            JobLevel.Junior,
            3);

        dbContext.JobPosts.AddRange(latest, withoutExpiry, earliest);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);
        var ascending = await service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { ExpiredDate = "asc" });
        var descending = await service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { ExpiredDate = "desc" });

        Assert.Equal(
            new[] { earliest.Id, latest.Id, withoutExpiry.Id },
            ascending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { latest.Id, earliest.Id, withoutExpiry.Id },
            descending.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task GetJobPostListAsync_RejectsInvalidExpiredDateFilter()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var service = new JobPostService(dbContext);

        var exception = await Assert.ThrowsAsync<JobPostException>(() => service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { ExpiredDate = "31/10/2030" }));

        Assert.Equal("JOB_POST_INVALID_EXPIRED_DATE_FILTER", exception.Code);
        Assert.Equal(["expiredDate"], exception.Fields);
    }

    private static DateTimeOffset CreateVietnamExpiry(DateOnly finalDay)
    {
        var nextDayAtMidnightInVietnam = new DateTimeOffset(
            finalDay.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.FromHours(7));

        return nextDayAtMidnightInVietnam.ToUniversalTime();
    }

    private static JobPost CreateJobPost(
        string title,
        JobPostStatus status,
        Guid departmentId,
        JobLevel jobLevel,
        int createdAtOffset)
    {
        return new JobPost
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = status,
            DepartmentId = departmentId,
            JobLevel = jobLevel,
            Skills = [],
            CreatedAt = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero)
                .AddMinutes(createdAtOffset)
        };
    }

    private sealed class JobPostFilterTestDbContext : AppDbContext
    {
        public JobPostFilterTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Content);
            modelBuilder.Entity<Product>().Ignore(product => product.Images);

            var skillsConverter = new ValueConverter<List<string>, string>(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!);

            modelBuilder.Entity<JobPost>()
                .Property(jobPost => jobPost.Skills)
                .HasConversion(skillsConverter);
        }
    }
}
