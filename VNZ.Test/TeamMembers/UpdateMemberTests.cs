using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class UpdateMemberTests
{
    [Fact]
    public async Task UpdateMemberAsync_UpdatesUnpublishedMember()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var memberId = Guid.NewGuid();
        var originalCreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var joinedDate = new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);

        dbContext.Users.Add(new User
        {
            Id = memberId,
            FullName = "Nguyen Van A",
            Email = "a@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Backend Developer",
            JobLevel = JobLevel.Junior,
            JoinedDate = originalCreatedAt,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = false,
            DisplayOrder = null,
            CreateAt = originalCreatedAt
        });
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.UpdateMemberAsync(
            memberId,
            new Request.UpdateTeamMemberRequest
            {
                FullName = "  Nguyen Van B  ",
                Email = "  B@VNZ.VN  ",
                Position = "  Senior Backend Developer  ",
                JobLevel = "Senior",
                JoinedDate = joinedDate,
                EmploymentStatus = "Resigned",
                IsPublished = false,
                Hometown = "  Ha Noi  ",
                BackgroundUrl = "  https://maps.example/ha-noi  "
            });

        var savedMember = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(member => member.Id == memberId);

        Assert.Equal("Nguyen Van B", response.FullName);
        Assert.Equal("b@vnz.vn", response.Email);
        Assert.Equal("Senior Backend Developer", response.Position);
        Assert.Equal("Senior", response.JobLevel);
        Assert.Equal(joinedDate, response.JoinedDate);
        Assert.Equal("Đã nghỉ việc", response.EmploymentStatus);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.NotNull(response.UpdatedAt);

        Assert.Equal("Nguyen Van B", savedMember.FullName);
        Assert.Equal("b@vnz.vn", savedMember.Email);
        Assert.Equal(EmploymentStatus.Resigned, savedMember.EmploymentStatus);
        Assert.Equal("Ha Noi", savedMember.Hometown);
        Assert.Equal("https://maps.example/ha-noi", savedMember.BackgroundUrl);
        Assert.False(savedMember.IsPublished);
        Assert.Null(savedMember.DisplayOrder);
        Assert.Equal(originalCreatedAt, savedMember.CreateAt);
        Assert.NotNull(savedMember.UpdatedAt);
    }
}
