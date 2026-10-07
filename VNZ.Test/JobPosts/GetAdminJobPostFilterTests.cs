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
    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetJobPostListAsync_AppliesMultiSelectFiltersBeforeDateSortingAndPagination(string direction)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);

        var departmentA = new Department { Id = Guid.NewGuid(), Name = "Kỹ thuật", CreatedAt = DateTimeOffset.UtcNow };
        var departmentB = new Department { Id = Guid.NewGuid(), Name = "Sản phẩm", CreatedAt = DateTimeOffset.UtcNow };

        var expectedOpen = CreateJobPost("Backend A", JobPostStatus.Open, departmentA.Id, JobLevel.Junior, 3);
        var expectedClosed = CreateJobPost("Backend B", JobPostStatus.Closed, departmentB.Id, JobLevel.Senior, 2);
        expectedOpen.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));
        expectedClosed.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 11, 30));
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
            ExpiredDate = direction,
            Page = 2,
            PageSize = 1
        });

        var expectedJobPost = direction == "asc" ? expectedClosed : expectedOpen;

        Assert.Equal(2, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Equal(2, response.Total);
        Assert.Equal(2, response.TotalPages);
        Assert.Single(response.Items);
        Assert.Equal(expectedJobPost.Id, response.Items[0].Id);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task GetJobPostListAsync_KeepsDefaultSortingWhenExpiredDateQueryIsEmpty(string? value)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var older = CreateJobPost(
            "Older job",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            1);
        older.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 11, 30));

        var newer = CreateJobPost(
            "Newer job",
            JobPostStatus.Open,
            Guid.NewGuid(),
            JobLevel.Junior,
            2);
        newer.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));

        dbContext.JobPosts.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);

        var response = await service.GetJobPostListAsync(new Request.GetJobPostListRequest
        {
            ExpiredDate = value
        });

        Assert.Equal(2, response.Total);
        Assert.Equal(new[] { newer.Id, older.Id }, response.Items.Select(item => item.Id));
        Assert.Equal(new DateOnly(2030, 10, 31), response.Items[0].ExpiredDate);
    }

    [Theory]
    [InlineData("asc", "desc")]
    [InlineData(" ASC ", " DESC ")]
    public async Task GetJobPostListAsync_SortsByExpiredDateAndPlacesMissingDatesLast(
        string ascendingValue,
        string descendingValue)
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
            new Request.GetJobPostListRequest { ExpiredDate = ascendingValue });
        var descending = await service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { ExpiredDate = descendingValue });

        Assert.Equal(3, ascending.Total);
        Assert.Equal(3, descending.Total);
        Assert.Equal(
            new[] { earliest.Id, latest.Id, withoutExpiry.Id },
            ascending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { latest.Id, earliest.Id, withoutExpiry.Id },
            descending.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("2030-10-31")]
    [InlineData("31/10/2030")]
    [InlineData("2030-13-01")]
    [InlineData("ascending")]
    public async Task GetJobPostListAsync_RejectsUnsupportedExpiredDateSortValues(string value)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var service = new JobPostService(dbContext);

        var exception = await Assert.ThrowsAsync<JobPostException>(() => service.GetJobPostListAsync(
            new Request.GetJobPostListRequest { ExpiredDate = value }));

        Assert.Equal("JOB_POST_INVALID_EXPIRED_DATE_FILTER", exception.Code);
        Assert.Equal(["expiredDate"], exception.Fields);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("asc", false)]
    [InlineData("desc", false)]
    [InlineData("asc", true)]
    [InlineData("desc", true)]
    public async Task GetJobPostListAsync_OrdersEqualDateKeysByIdDescendingBeforePagination(
        string? direction,
        bool withoutExpiry)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var lowerId = CreateJobPost("Lower ID", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        var higherId = CreateJobPost("Higher ID", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        lowerId.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        higherId.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        if (direction is not null)
        {
            higherId.CreatedAt = lowerId.CreatedAt.AddDays(-1);
        }

        if (!withoutExpiry)
        {
            lowerId.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));
            higherId.ExpiredAt = lowerId.ExpiredAt;
        }

        dbContext.JobPosts.AddRange(lowerId, higherId);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);
        var request = new Request.GetJobPostListRequest
        {
            ExpiredDate = direction,
            PageSize = 1
        };
        var firstPage = await service.GetJobPostListAsync(request);
        request.Page = 2;
        var secondPage = await service.GetJobPostListAsync(request);

        Assert.Equal(2, firstPage.Total);
        Assert.Equal(2, secondPage.Total);
        Assert.Equal(higherId.Id, Assert.Single(firstPage.Items).Id);
        Assert.Equal(lowerId.Id, Assert.Single(secondPage.Items).Id);
    }

    [Theory]
    [InlineData("asc", "Earlier,Middle,Later")]
    [InlineData("desc", "Later,Middle,Earlier")]
    public async Task GetJobPostListAsync_SortsByStoredExpiryTimestampWhenDisplayedDatesMatch(
        string direction,
        string expectedTitles)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var earlier = CreateJobPost("Earlier", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        var middle = CreateJobPost("Middle", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        var later = CreateJobPost("Later", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        earlier.Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        middle.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        later.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var finalDay = new DateOnly(2030, 10, 31);
        earlier.ExpiredAt = CreateVietnamExpiry(finalDay);
        middle.ExpiredAt = earlier.ExpiredAt.Value.AddMinutes(1);
        later.ExpiredAt = earlier.ExpiredAt.Value.AddMinutes(2);

        dbContext.JobPosts.AddRange(middle, later, earlier);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);
        var response = await service.GetJobPostListAsync(new Request.GetJobPostListRequest
        {
            ExpiredDate = direction
        });

        Assert.Equal(expectedTitles.Split(','), response.Items.Select(item => item.Title));
        Assert.All(response.Items, item => Assert.Equal(finalDay, item.ExpiredDate));
    }

    [Fact]
    public async Task GetJobPostListAsync_SerializesExpiredDateAsCalendarDateAndPreservesNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new JobPostFilterTestDbContext(options);
        var withExpiry = CreateJobPost("With expiry", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 2);
        var withoutExpiry = CreateJobPost("Without expiry", JobPostStatus.Draft, Guid.NewGuid(), JobLevel.Junior, 1);
        withExpiry.ExpiredAt = CreateVietnamExpiry(new DateOnly(2030, 10, 31));

        dbContext.JobPosts.AddRange(withoutExpiry, withExpiry);
        await dbContext.SaveChangesAsync();

        var service = new JobPostService(dbContext);
        var response = await service.GetJobPostListAsync(new Request.GetJobPostListRequest());
        var envelope = VNZ.Service.Models.ResponseBuilder.SuccessResponse(response, "Success");
        var jsonOptions = new Microsoft.AspNetCore.Mvc.JsonOptions().JsonSerializerOptions;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(envelope, jsonOptions));
        var items = json.RootElement.GetProperty("data").GetProperty("items");

        Assert.Equal("2030-10-31", items[0].GetProperty("expiredDate").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("expiredDate").ValueKind);
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
