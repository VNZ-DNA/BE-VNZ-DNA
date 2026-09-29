using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class CreateMemberTests
{
    [Theory]
    [InlineData("  TAN  ", "TAN")]
    [InlineData("  Tân  ", "Tân")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public async Task CreateMemberAsync_CreatesWorkingUnpublishedMember(string? displayName, string? expectedDisplayName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var createdBy = Guid.NewGuid();
        var joinedDate = new DateTimeOffset(2026, 9, 21, 0, 0, 0, TimeSpan.Zero);
        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.CreateMemberAsync(
            new Request.CreateTeamMemberRequest
            {
                FullName = "  Nguyen Van A  ",
                DisplayName = displayName,
                Email = "  A@VNZ.VN  ",
                Position = "  Backend Developer  ",
                JobLevel = "Junior",
                JoinedDate = joinedDate,
                Hometown = "  Da Nang  ",
                BackgroundUrl = "  https://maps.example/da-nang  "
            },
            createdBy);

        var savedMember = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(member => member.Id == response.Id);

        Assert.Equal("Nguyen Van A", response.FullName);
        Assert.Equal(expectedDisplayName, response.DisplayName);
        Assert.Equal(expectedDisplayName, savedMember.DisplayName);
        Assert.Equal("a@vnz.vn", response.Email);
        Assert.Equal("Backend Developer", response.Position);
        Assert.Equal("Junior", response.JobLevel);
        Assert.Equal("Đang làm việc", response.EmploymentStatus);
        Assert.Equal(joinedDate, response.JoinedDate);
        Assert.True(response.IsActive);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Null(response.UpdatedAt);

        Assert.Equal(createdBy, savedMember.CreatedBy);
        Assert.Null(savedMember.RoleId);
        Assert.Equal(EmploymentStatus.Working, savedMember.EmploymentStatus);
        Assert.True(savedMember.IsActive);
        Assert.False(savedMember.IsPublished);
        Assert.Null(savedMember.DisplayOrder);
        Assert.Equal("Da Nang", savedMember.Hometown);
        Assert.Equal("https://maps.example/da-nang", savedMember.BackgroundUrl);
        Assert.NotEqual("Vnz@123456", savedMember.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Vnz@123456", savedMember.PasswordHash));
        Assert.NotEqual(default, savedMember.CreateAt);
    }
}
