using System.Text.Json;
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
    public async Task PublicLists_ReturnOnlyWorkingPublishedMembers()
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

        var featured = await service.GetFeaturedMembersAsync();
        var response = await service.GetPublicMemberListAsync();

        Assert.Equal(2, featured.Total);
        Assert.Equal(response.Select(member => member.Id), featured.Items.Select(member => member.Id));

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
    public async Task PublicLists_FeaturedLimitsToSixButSelectorReturnsAllInSameDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new TestAppDbContext(options);
        var date = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        var ids = Enumerable.Range(1, 8)
            .Select(index => Guid.Parse($"10000000-0000-0000-0000-{index:D12}"))
            .ToArray();
        var members = new[]
        {
            CreateMember(ids[0], "A", EmploymentStatus.Working, true, 1, date),
            CreateMember(ids[1], "B", EmploymentStatus.Working, true, 1, date),
            CreateMember(ids[2], "C", EmploymentStatus.Working, true, 2, date),
            CreateMember(ids[3], "D", EmploymentStatus.Working, true, 3, date),
            CreateMember(ids[4], "E", EmploymentStatus.Working, true, null, date),
            CreateMember(ids[5], "F", EmploymentStatus.Working, true, null, date.AddDays(1)),
            CreateMember(ids[6], "G", EmploymentStatus.Working, true, null, date.AddDays(1)),
            CreateMember(ids[7], "H", EmploymentStatus.Working, true, null, date.AddDays(-1))
        };
        members[1].Position = "Developer";
        members[1].DisplayName = "TÂN";
        members[1].JobLevel = JobLevel.Lead;
        members[1].Hometown = "Nam Định";
        members[1].BackgroundUrl = "https://cdn.vnzdna.com/members/backgrounds/nam-dinh.png";
        members[1].AvatarUrl = "avatar.png";
        members[1].IsActive = false;
        dbContext.Users.AddRange(members.Reverse());
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var featured = await service.GetFeaturedMembersAsync();
        var selector = await service.GetPublicMemberListAsync();

        Assert.Equal(8, featured.Total);
        Assert.Equal(new[] { ids[1], ids[0], ids[2], ids[3], ids[6], ids[5] },
            featured.Items.Select(member => member.Id));
        Assert.Equal(new[] { ids[1], ids[0], ids[2], ids[3], ids[6], ids[5], ids[4], ids[7] },
            selector.Select(member => member.Id));
        Assert.Equal("Lead", featured.Items[0].JobLevel);
        Assert.Equal("TAN", featured.Items[0].DisplayName);
        Assert.Equal("B", featured.Items[0].FullName);
        Assert.Null(featured.Items[1].DisplayName);
        Assert.Equal(members[1].Hometown, featured.Items[0].Hometown);
        Assert.Equal(members[1].BackgroundUrl, featured.Items[0].BackgroundUrl);
        Assert.Equal("avatar.png", featured.Items[0].AvatarUrl);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var featuredJson = JsonSerializer.SerializeToElement(featured, jsonOptions);
        Assert.Equal(8, featuredJson.GetProperty("total").GetInt32());
        Assert.Equal(6, featuredJson.GetProperty("items").GetArrayLength());
        Assert.Equal("TAN", featuredJson.GetProperty("items")[0].GetProperty("displayName").GetString());
        Assert.Equal(JsonValueKind.Null, featuredJson.GetProperty("items")[1].GetProperty("displayName").ValueKind);
        Assert.Equal(new[] { "avatarUrl", "backgroundUrl", "displayName", "fullName", "hometown", "id", "jobLevel", "position" },
            featuredJson.GetProperty("items")[0].EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        var selectorJson = JsonSerializer.SerializeToElement(selector, jsonOptions);
        Assert.Equal(8, selectorJson.GetArrayLength());
        foreach (var member in selectorJson.EnumerateArray())
            Assert.Equal(new[] { "avatarUrl", "fullName", "id", "position" },
                member.EnumerateObject().Select(property => property.Name).OrderBy(name => name));

        var lastDetail = await service.GetPublicMemberByIdAsync(selector[^1].Id);
        Assert.Equal(ids[7], lastDetail.Id);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData("Tân", "Tan")]
    [InlineData("TÂN", "TAN")]
    [InlineData("Đặng Đỗ", "Dang Do")]
    [InlineData("đặng", "dang")]
    [InlineData("Ta\u0302n Đa\u0323\u0306ng", "Tan Dang")]
    [InlineData("Nguyễn-Đạt 99", "Nguyen-Dat 99")]
    [InlineData("TAN", "TAN")]
    [InlineData(null, null)]
    public async Task GetFeaturedMembersAsync_RemovesDisplayNameDiacriticsWithoutChangingStoredProfile(
        string? displayName, string? expectedDisplayName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new TestAppDbContext(options);
        var member = CreateMember(Guid.NewGuid(), "Trần Đình Thiên Tân", EmploymentStatus.Working,
            true, 1, DateTimeOffset.UtcNow);
        member.DisplayName = displayName;
        dbContext.Users.Add(member);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var response = await service.GetFeaturedMembersAsync();

        var item = Assert.Single(response.Items);
        Assert.Equal(expectedDisplayName, item.DisplayName);
        Assert.Equal("Trần Đình Thiên Tân", item.FullName);
        Assert.Empty(dbContext.ChangeTracker.Entries());
        Assert.Equal(displayName, (await dbContext.Users.AsNoTracking().SingleAsync()).DisplayName);
        Assert.Equal(displayName, (await service.GetMemberByIdAsync(member.Id)).DisplayName);
        var json = JsonSerializer.SerializeToElement(item, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(expectedDisplayName, json.GetProperty("displayName").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(6)]
    public async Task PublicLists_ReturnAvailableMembersWithoutPadding(int count)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new TestAppDbContext(options);
        for (var index = 0; index < count; index++)
            dbContext.Users.Add(CreateMember(Guid.NewGuid(), $"Member {index}", EmploymentStatus.Working,
                true, index + 1, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();
        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var featured = await service.GetFeaturedMembersAsync();
        var selector = await service.GetPublicMemberListAsync();

        Assert.Equal(count, featured.Total);
        Assert.Equal(count, featured.Items.Count);
        Assert.Equal(count, selector.Count);
        Assert.Equal(selector.Select(member => member.Id), featured.Items.Select(member => member.Id));
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
            DisplayName = "TAN",
            Email = "member@vnz.vn",
            PasswordHash = "password-hash",
            Position = "Product Designer",
            JobLevel = JobLevel.Senior,
            AvatarUrl = "avatar.png",
            Hometown = "Kon Tum",
            BackgroundUrl = "https://cdn.vnzdna.com/members/backgrounds/kon-tum.png",
            AnimationUrl = "https://cdn.vnzdna.com/members/animations/member-a.webm",
            AudioUrl = "https://cdn.vnzdna.com/members/audio/tan.mp3",
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
        Assert.Equal("Senior", response.JobLevel);
        Assert.Equal("avatar.png", response.AvatarUrl);
        Assert.Equal("Kon Tum", response.Hometown);
        Assert.Equal("https://cdn.vnzdna.com/members/backgrounds/kon-tum.png", response.BackgroundUrl);
        Assert.Equal("https://cdn.vnzdna.com/members/animations/member-a.webm", response.AnimationUrl);
        Assert.Equal("https://cdn.vnzdna.com/members/audio/tan.mp3", response.AudioUrl);
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

        var json = JsonSerializer.SerializeToElement(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(
            new[] { "id", "fullName", "position", "jobLevel", "avatarUrl", "hometown", "backgroundUrl",
                "hobbies", "joinedDate", "personalQuote", "animationUrl", "audioUrl" }.OrderBy(name => name),
            json.EnumerateObject().Select(property => property.Name).OrderBy(name => name));
        Assert.Equal(response.BackgroundUrl, json.GetProperty("backgroundUrl").GetString());
        Assert.Equal(response.AudioUrl, json.GetProperty("audioUrl").GetString());
        Assert.Equal(joinedDate, json.GetProperty("joinedDate").GetDateTimeOffset());
    }

    [Fact]
    public async Task GetPublicMemberByIdAsync_ReturnsSelectedMembersMediaAndPreservesNulls()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new TestAppDbContext(options);
        var first = CreateMember(Guid.NewGuid(), "A", EmploymentStatus.Working, true, 1, DateTimeOffset.UtcNow);
        first.BackgroundUrl = "https://cdn.vnzdna.com/a.png";
        first.AnimationUrl = "https://cdn.vnzdna.com/a.webm";
        first.AudioUrl = "https://cdn.vnzdna.com/a.mp3";
        var second = CreateMember(Guid.NewGuid(), "B", EmploymentStatus.Working, true, 2, DateTimeOffset.UtcNow);
        second.IsActive = false;
        dbContext.Users.AddRange(first, second);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        var service = new TeamMemberService(dbContext, new VNZ.Test.TestMediaService());

        var firstResponse = await service.GetPublicMemberByIdAsync(first.Id);
        var secondResponse = await service.GetPublicMemberByIdAsync(second.Id);

        Assert.Equal(first.BackgroundUrl, firstResponse.BackgroundUrl);
        Assert.Equal(first.AnimationUrl, firstResponse.AnimationUrl);
        Assert.Equal(first.AudioUrl, firstResponse.AudioUrl);
        Assert.Equal(second.Id, secondResponse.Id);
        var json = JsonSerializer.SerializeToElement(secondResponse, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        foreach (var field in new[] { "backgroundUrl", "animationUrl", "audioUrl", "jobLevel", "joinedDate" })
            Assert.Equal(JsonValueKind.Null, json.GetProperty(field).ValueKind);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetPublicMemberByIdAsync_RejectsUnpublishedResignedAdminAndMissingMembers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new TestAppDbContext(options);
        var resignedId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var unpublishedId = Guid.NewGuid();
        dbContext.Users.AddRange(
            CreateMember(
                unpublishedId,
                "Unpublished",
                EmploymentStatus.Working,
                false,
                1,
                DateTimeOffset.UtcNow),
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
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(unpublishedId));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(adminId));
        await Assert.ThrowsAsync<NotFoundException>(() => service.GetPublicMemberByIdAsync(Guid.Empty));
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
