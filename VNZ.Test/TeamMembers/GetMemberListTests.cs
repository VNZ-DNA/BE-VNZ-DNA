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

        var service = new TeamMemberService(dbContext);

        var response = await service.GetMemberListAsync(new Request.GetTeamMemberListRequest());

        Assert.Equal(3, response.Total);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(3, response.Items.Count);
        Assert.Equal(firstMemberId, response.Items[0].Id);
        Assert.Equal(secondMemberId, response.Items[1].Id);
        Assert.Equal("Nguyen Van C", response.Items[2].FullName);
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
    }
}
