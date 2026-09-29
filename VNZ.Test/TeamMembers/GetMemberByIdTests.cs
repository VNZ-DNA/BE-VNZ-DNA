using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class GetMemberByIdTests
{
    [Fact]
    public async Task GetMemberByIdAsync_ReturnsMemberDetails()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var memberId = Guid.NewGuid();
        var joinedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var createdAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 9, 22, 8, 30, 0, TimeSpan.Zero);

        dbContext.Users.Add(new User
        {
            Id = memberId,
            FullName = "Nguyen Van A",
            DisplayName = "TAN",
            Email = "member@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Backend Developer",
            JobLevel = JobLevel.Senior,
            AvatarUrl = "avatar.png",
            AnimationUrl = "animation.gif",
            AudioUrl = "audio.mp3",
            Hometown = "Ha Noi",
            BackgroundUrl = "https://maps.example/ha-noi",
            Hobbies = "Doc sach",
            PersonalQuote = "Lam dung ngay tu dau",
            JoinedDate = joinedDate,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Resigned,
            IsPublished = false,
            DisplayOrder = null,
            CreateAt = createdAt,
            UpdatedAt = updatedAt
        });
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetMemberByIdAsync(memberId);

        Assert.Equal(memberId, response.Id);
        Assert.Equal("Nguyen Van A", response.FullName);
        Assert.Equal("TAN", response.DisplayName);
        Assert.Equal("member@vnz.vn", response.Email);
        Assert.Equal("Backend Developer", response.Position);
        Assert.Equal("Senior", response.JobLevel);
        Assert.Equal("avatar.png", response.AvatarUrl);
        Assert.Equal("animation.gif", response.AnimationUrl);
        Assert.Equal("audio.mp3", response.AudioUrl);
        Assert.Equal("Ha Noi", response.Hometown);
        Assert.Equal("https://maps.example/ha-noi", response.BackgroundUrl);
        Assert.Equal("Doc sach", response.Hobbies);
        Assert.Equal("Lam dung ngay tu dau", response.PersonalQuote);
        Assert.Equal(joinedDate, response.JoinedDate);
        Assert.Equal("Đã nghỉ việc", response.EmploymentStatus);
        Assert.True(response.IsActive);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
    }
}
