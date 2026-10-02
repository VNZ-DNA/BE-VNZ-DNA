using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.TeamMembers;
using TeamMemberService = VNZ.Service.TeamMembers.Service;
using Xunit;

namespace VNZ.Test.TeamMembers;

public class GetMemberListTests
{
    [Fact]
    public async Task GetMemberListAsync_ReturnsTeamMembersInExpectedOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        var firstMemberId = Guid.NewGuid();
        var secondMemberId = Guid.NewGuid();

        dbContext.Users.AddRange(
            new User
            {
                Id = firstMemberId,
                FullName = "Nguyen Van A",
                DisplayName = "TAN",
                Email = "a@vnz.vn",
                PasswordHash = "password",
                EmploymentStatus = EmploymentStatus.Working,
                IsActive = true,
                IsPublished = true,
                DisplayOrder = 1,
                CreateAt = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new User
            {
                Id = secondMemberId,
                FullName = "Nguyen Van B",
                Email = "b@vnz.vn",
                PasswordHash = "password",
                EmploymentStatus = EmploymentStatus.Working,
                IsActive = true,
                IsPublished = true,
                DisplayOrder = 2,
                CreateAt = new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero)
            },
            new User
            {
                FullName = "Nguyen Van C",
                Email = "c@vnz.vn",
                PasswordHash = "password",
                EmploymentStatus = EmploymentStatus.Working,
                IsActive = true,
                IsPublished = false,
                CreateAt = new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero)
            },
            new User
            {
                FullName = "Admin User",
                Email = "admin@vnz.vn",
                PasswordHash = "password",
                RoleId = Guid.NewGuid(),
                EmploymentStatus = EmploymentStatus.Working,
                IsActive = true,
                IsPublished = true,
                DisplayOrder = 0,
                CreateAt = new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero)
            });

        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest());

        Assert.Equal(3, response.Total);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(3, response.Items.Count);
        Assert.Equal(firstMemberId, response.Items[0].Id);
        Assert.Equal("TAN", response.Items[0].DisplayName);
        Assert.Null(response.Items[1].DisplayName);
        Assert.Equal(secondMemberId, response.Items[1].Id);
        Assert.Equal("Nguyen Van C", response.Items[2].FullName);
    }

    [Fact]
    public async Task GetMemberListAsync_AppliesSearchAcrossNameAndEmailOnlyWithMultipleStatuses()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        dbContext.Users.AddRange(
            CreateMember(
                "Backend Alice",
                "alice@vnz.vn",
                "Product Designer",
                EmploymentStatus.Working),
            CreateMember(
                "Tran Van B",
                "backend@vnz.vn",
                "Human Resources",
                EmploymentStatus.Resigned),
            CreateMember(
                "Le Van C",
                "c@vnz.vn",
                "Backend Engineer",
                EmploymentStatus.Working),
            CreateMember(
                "Other Member",
                "other@vnz.vn",
                "Finance",
                EmploymentStatus.Resigned));

        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest
        {
            Search = " backend ",
            Status = [nameof(EmploymentStatus.Working), nameof(EmploymentStatus.Resigned)],
            PageSize = 10
        });

        Assert.Equal(2, response.Total);
        Assert.Equal(2, response.Items.Count);
        Assert.Contains(response.Items, member => member.FullName == "Backend Alice");
        Assert.Contains(response.Items, member => member.Email == "backend@vnz.vn");
        Assert.DoesNotContain(response.Items, member => member.Position == "Backend Engineer");
    }

    [Fact]
    public async Task GetMemberListAsync_CombinesSearchWithSelectedStatusesUsingAnd()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        dbContext.Users.AddRange(
            CreateMember(
                "Working Backend",
                "working@vnz.vn",
                "Developer",
                EmploymentStatus.Working),
            CreateMember(
                "Resigned Backend",
                "resigned@vnz.vn",
                "Developer",
                EmploymentStatus.Resigned));

        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest
        {
            Search = "backend",
            Status = [nameof(EmploymentStatus.Resigned)],
            PageSize = 10
        });

        var member = Assert.Single(response.Items);
        Assert.Equal("Resigned Backend", member.FullName);
        Assert.Equal(1, response.Total);
    }

    [Fact]
    public async Task GetMemberListAsync_AppliesMultiplePositionAndJobLevelFilters()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        dbContext.Users.AddRange(
            CreateMember(
                "Developer Junior",
                "junior@vnz.vn",
                "Developer",
                EmploymentStatus.Working,
                JobLevel.Junior),
            CreateMember(
                "Designer Senior",
                "senior@vnz.vn",
                "Designer",
                EmploymentStatus.Working,
                JobLevel.Senior),
            CreateMember(
                "Developer Lead",
                "lead@vnz.vn",
                "Developer",
                EmploymentStatus.Resigned,
                JobLevel.Lead),
            CreateMember(
                "Developer Intern",
                "intern@vnz.vn",
                "Developer",
                EmploymentStatus.Working,
                JobLevel.Intern));

        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest
        {
            Position = ["Developer", "Designer"],
            JobLevel = [nameof(JobLevel.Junior), nameof(JobLevel.Senior)],
            Status = [nameof(EmploymentStatus.Working), nameof(EmploymentStatus.Resigned)],
            PageSize = 10
        });

        Assert.Equal(2, response.Total);
        Assert.Contains(response.Items, member => member.FullName == "Developer Junior");
        Assert.Contains(response.Items, member => member.FullName == "Designer Senior");
    }

    [Fact]
    public async Task GetFilterOptionsAsync_ReturnsAllPositionsAndJobLevels()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        var adminMember = CreateMember(
            "Admin",
            "admin@vnz.vn",
            "Ignored",
            EmploymentStatus.Working,
            JobLevel.Lead);
        adminMember.RoleId = Guid.NewGuid();

        dbContext.Users.AddRange(
            CreateMember(
                "Developer",
                "developer@vnz.vn",
                " Developer ",
                EmploymentStatus.Working,
                JobLevel.Junior),
            CreateMember(
                "Designer",
                "designer@vnz.vn",
                "Designer",
                EmploymentStatus.Resigned,
                JobLevel.Senior),
            adminMember);

        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetFilterOptionsAsync();

        Assert.Equal(["Designer", "Developer"], response.Positions);
        Assert.Equal(
            ["Intern", "Fresher", "Junior", "Middle", "Senior", "Lead"],
            response.JobLevels);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task GetMemberListAsync_AcceptsSupportedPageSizes(int pageSize)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest
        {
            PageSize = pageSize
        });

        Assert.Equal(pageSize, response.PageSize);
    }

    [Fact]
    public async Task GetMemberListAsync_RejectsUnsupportedPageSize()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var exception = await Assert.ThrowsAsync<VNZ.Service.Exceptions.TeamMemberException>(() =>
            service.GetMemberListAsync(new Request.GetTeamMemberListRequest
            {
                PageSize = 25
            }));

        Assert.Equal("MEMBER_QUERY_INVALID", exception.Code);
    }

    private static User CreateMember(
        string fullName,
        string email,
        string position,
        EmploymentStatus employmentStatus,
        JobLevel? jobLevel = null)
    {
        return new User
        {
            FullName = fullName,
            Email = email,
            Position = position,
            JobLevel = jobLevel,
            PasswordHash = "password",
            EmploymentStatus = employmentStatus,
            IsActive = true,
            IsPublished = false,
            CreateAt = DateTimeOffset.UtcNow
        };
    }
}

internal sealed class TestAppDbContext : AppDbContext
{
    public TestAppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<JobPost>().Ignore(jobPost => jobPost.Skills);
        modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
        modelBuilder.Entity<Product>().Ignore(product => product.Content);
        modelBuilder.Entity<Product>().Ignore(product => product.Images);
    }
}
