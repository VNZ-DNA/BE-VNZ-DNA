using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class UpdateMemberTests
{
    [Theory]
    [InlineData("  TAN  ", "TAN")]
    [InlineData("  Tân  ", "Tân")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    public async Task UpdateMemberAsync_UpdatesUnpublishedMember(string? displayName, string? expectedDisplayName)
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
            DisplayName = "Old nickname",
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
                DisplayName = displayName,
                Email = "  B@VNZ.VN  ",
                Position = "  Senior Backend Developer  ",
                JobLevel = "Senior",
                JoinedDate = joinedDate,
                EmploymentStatus = "Resigned",
                Hometown = "  Ha Noi  ",
                Avatar = CreateFile("avatar.png"),
                Background = CreateFile("background.png"),
                Audio = CreateFile("audio.mp3")
            });

        var savedMember = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(member => member.Id == memberId);

        Assert.Equal("Nguyen Van B", response.FullName);
        Assert.Equal(expectedDisplayName, response.DisplayName);
        Assert.Equal(expectedDisplayName, savedMember.DisplayName);
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
        Assert.Equal("https://cdn.example.com/test-image.png", savedMember.AvatarUrl);
        Assert.Equal("https://cdn.example.com/test-background.png", savedMember.BackgroundUrl);
        Assert.Equal("https://cdn.example.com/test-audio.mp3", savedMember.AudioUrl);
        Assert.False(savedMember.IsPublished);
        Assert.Null(savedMember.DisplayOrder);
        Assert.Equal(originalCreatedAt, savedMember.CreateAt);
        Assert.NotNull(savedMember.UpdatedAt);
    }

    [Fact]
    public async Task UpdateMemberAsync_RemovesSelectedMediaReferences()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var memberId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.Users.Add(new User
        {
            Id = memberId,
            FullName = "Nguyen Van A",
            Email = "a@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Backend Developer",
            JobLevel = JobLevel.Junior,
            JoinedDate = createdAt,
            AvatarUrl = "https://cdn.example.com/avatar.png",
            BackgroundUrl = "https://cdn.example.com/background.png",
            AudioUrl = "https://cdn.example.com/audio.mp3",
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = false,
            CreateAt = createdAt
        });
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var response = await service.UpdateMemberAsync(
            memberId,
            new Request.UpdateTeamMemberRequest
            {
                FullName = "Nguyen Van A",
                Email = "a@vnz.vn",
                Position = "Backend Developer",
                JobLevel = "Junior",
                JoinedDate = createdAt,
                EmploymentStatus = "Working",
                AvatarAction = "remove",
                BackgroundAction = "remove",
                AudioAction = "remove"
            });

        Assert.Null(response.AvatarUrl);
        Assert.Null(response.BackgroundUrl);
        Assert.Null(response.AudioUrl);
    }

    [Fact]
    public async Task UpdateMemberAsync_RejectsRemoveActionWithSameMediaFile()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var memberId = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.Users.Add(new User
        {
            Id = memberId,
            FullName = "Nguyen Van A",
            Email = "a@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Backend Developer",
            JobLevel = JobLevel.Junior,
            JoinedDate = createdAt,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = false,
            CreateAt = createdAt
        });
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var exception = await Assert.ThrowsAsync<TeamMemberException>(() => service.UpdateMemberAsync(
            memberId,
            new Request.UpdateTeamMemberRequest
            {
                FullName = "Nguyen Van A",
                Email = "a@vnz.vn",
                Position = "Backend Developer",
                JobLevel = "Junior",
                JoinedDate = createdAt,
                EmploymentStatus = "Working",
                AvatarAction = "remove",
                Avatar = CreateFile("avatar.png")
            }));

        Assert.Equal("MEMBER_MEDIA_ACTION_INVALID", exception.Code);
        Assert.Equal(["avatarAction", "avatar"], exception.Fields);
    }

    private static IFormFile CreateFile(string fileName)
    {
        return new FormFile(new MemoryStream([1, 2, 3]), 0, 3, "file", fileName);
    }
}
