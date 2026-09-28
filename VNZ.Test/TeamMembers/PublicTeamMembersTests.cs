using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using TeamMemberService = VNZ.Service.TeamMembers.Service;
using Xunit;

namespace VNZ.Test.TeamMembers;

public class PublicTeamMembersTests
{
    [Fact]
    public async Task GetFeaturedMembersAsync_ReturnsOnlyWorkingPublishedMembers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        dbContext.Users.AddRange(
            CreateMember(
                firstId,
                "Member A",
                EmploymentStatus.Working,
                true,
                1,
                new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)),
            CreateMember(
                secondId,
                "Member B",
                EmploymentStatus.Working,
                true,
                2,
                new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero)),
            CreateMember(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "Unpublished",
                EmploymentStatus.Working,
                false,
                null,
                new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero)),
            CreateMember(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "Resigned",
                EmploymentStatus.Resigned,
                true,
                0,
                new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero)),
            CreateMember(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Admin",
                EmploymentStatus.Working,
                true,
                0,
                new DateTimeOffset(2026, 9, 5, 0, 0, 0, TimeSpan.Zero),
                Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetFeaturedMembersAsync();

        Assert.Equal(2, response.Count);
        Assert.Equal(firstId, response[0].Id);
        Assert.Equal(secondId, response[1].Id);
        Assert.Equal("Member A", response[0].FullName);
        Assert.Null(response[0].Position);
        Assert.DoesNotContain(response, member => member.FullName == "Unpublished");
        Assert.DoesNotContain(response, member => member.FullName == "Resigned");
        Assert.DoesNotContain(response, member => member.FullName == "Admin");
    }

    [Fact]
    public async Task GetPublicMemberByIdAsync_ReturnsWorkingPublishedMemberWithoutSensitiveFields()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var memberId = Guid.NewGuid();
        var joinedDate = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        dbContext.Users.Add(new User
        {
            Id = memberId,
            FullName = "Member A",
            Email = "member@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Product Designer",
            AvatarUrl = "avatar.png",
            Hometown = "Kon Tum",
            Hobbies = "Vẽ tranh",
            PersonalQuote = "Đơn giản là đỉnh cao",
            JoinedDate = joinedDate,
            IsActive = true,
            EmploymentStatus = EmploymentStatus.Working,
            IsPublished = true,
            DisplayOrder = null,
            CreateAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetPublicMemberByIdAsync(memberId);

        Assert.Equal(memberId, response.Id);
        Assert.Equal("Member A", response.FullName);
        Assert.Equal("Product Designer", response.Position);
        Assert.Equal("avatar.png", response.AvatarUrl);
        Assert.Equal("Kon Tum", response.Hometown);
        Assert.Equal("Vẽ tranh", response.Hobbies);
        Assert.Equal(joinedDate, response.JoinedDate);
        Assert.Equal("Đơn giản là đỉnh cao", response.PersonalQuote);

        var propertyNames = typeof(VNZ.Service.TeamMembers.Response.PublicTeamMemberDetailResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain("Email", propertyNames);
        Assert.DoesNotContain("PasswordHash", propertyNames);
        Assert.DoesNotContain("IsActive", propertyNames);
        Assert.DoesNotContain("IsPublished", propertyNames);
        Assert.DoesNotContain("DisplayOrder", propertyNames);
    }

    [Fact]
    public async Task GetPublicMemberByIdAsync_RejectsResignedAdminAndMissingMembers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var resignedId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        dbContext.Users.AddRange(
            CreateMember(
                resignedId,
                "Resigned",
                EmploymentStatus.Resigned,
                true,
                1,
                DateTimeOffset.UtcNow),
            CreateMember(
                adminId,
                "Admin",
                EmploymentStatus.Working,
                true,
                1,
                DateTimeOffset.UtcNow,
                Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(
            dbContext,
            new VNZ.Test.TestMediaService());

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(resignedId));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(adminId));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(Guid.NewGuid()));
    }

    private static User CreateMember(
        Guid id,
        string fullName,
        EmploymentStatus employmentStatus,
        bool isPublished,
        int? displayOrder,
        DateTimeOffset createAt,
        Guid? roleId = null)
    {
        return new User
        {
            Id = id,
            FullName = fullName,
            Email = $"{id}@vnz.vn",
            PasswordHash = "password",
            RoleId = roleId,
            Position = null,
            IsActive = true,
            EmploymentStatus = employmentStatus,
            IsPublished = isPublished,
            DisplayOrder = displayOrder,
            CreateAt = createAt
        };
    }
}
