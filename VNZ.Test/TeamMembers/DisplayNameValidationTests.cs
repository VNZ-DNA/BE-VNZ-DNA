using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class DisplayNameValidationTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    public async Task CreateMemberAsync_ValidatesDisplayNameLengthAfterTrim(int length)
    {
        await using var db = CreateContext();
        var service = new TeamMemberService(db, new TestMediaService());
        var request = CreateRequest("  " + new string('A', length) + "  ");

        if (length == 101)
        {
            var error = await Assert.ThrowsAsync<TeamMemberException>(() => service.CreateMemberAsync(request, Guid.NewGuid()));
            Assert.Equal("MEMBER_VALIDATION_ERROR", error.Code);
            Assert.Equal(new[] { "displayName" }, error.Fields);
            Assert.Empty(await db.Users.AsNoTracking().ToListAsync());
            return;
        }

        var result = await service.CreateMemberAsync(request, Guid.NewGuid());
        db.ChangeTracker.Clear();
        Assert.Equal(new string('A', length), result.DisplayName);
        Assert.Equal(result.DisplayName, (await db.Users.SingleAsync()).DisplayName);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(101)]
    public async Task UpdateMemberAsync_ValidatesDisplayNameLengthBeforeChangingProfile(int length)
    {
        await using var db = CreateContext();
        var service = new TeamMemberService(db, new TestMediaService());
        var created = await service.CreateMemberAsync(CreateRequest("ORIGINAL"), Guid.NewGuid());
        var request = UpdateRequest("  " + new string('A', length) + "  ");

        if (length == 101)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateMemberAsync(created.Id, request));
            db.ChangeTracker.Clear();
            var unchanged = await db.Users.SingleAsync();
            Assert.Equal("ORIGINAL", unchanged.DisplayName);
            Assert.Equal(created.FullName, unchanged.FullName);
            Assert.Null(unchanged.UpdatedAt);
            return;
        }

        var result = await service.UpdateMemberAsync(created.Id, request);
        db.ChangeTracker.Clear();
        Assert.Equal(new string('A', length), result.DisplayName);
        Assert.Equal(result.DisplayName, (await db.Users.SingleAsync()).DisplayName);
    }

    [Fact]
    public async Task DisplayName_UnpublishPreservesValue_RepublishExposesUpdatedNameOnlyInFeatured()
    {
        await using var db = CreateContext();
        var service = new TeamMemberService(db, new TestMediaService());
        var created = await service.CreateMemberAsync(CreateRequest("ORIGINAL"), Guid.NewGuid());
        var publish = UpdateRequest("ORIGINAL");
        publish.IsPublished = true;
        await service.UpdateMemberAsync(created.Id, publish);

        var editWhilePublished = UpdateRequest("NEW");
        editWhilePublished.IsPublished = true;
        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateMemberAsync(created.Id, editWhilePublished));
        var unpublished = await service.UpdateMemberAsync(created.Id, new Request.UpdateTeamMemberRequest
        {
            IsPublished = false,
            DisplayName = new string('A', 101)
        });
        Assert.Equal("ORIGINAL", unpublished.DisplayName);
        Assert.False(unpublished.IsPublished);
        Assert.Empty((await service.GetFeaturedMembersAsync()).Items);

        var update = UpdateRequest("  TAN  ");
        update.IsPublished = true;
        await service.UpdateMemberAsync(created.Id, update);
        db.ChangeTracker.Clear();
        Assert.Equal("TAN", (await service.GetMemberByIdAsync(created.Id)).DisplayName);
        Assert.Equal("TAN", Assert.Single((await service.GetFeaturedMembersAsync()).Items).DisplayName);
        Assert.Equal("Updated full name", (await service.GetPublicMemberByIdAsync(created.Id)).FullName);
    }

    private static TestAppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    private static Request.CreateTeamMemberRequest CreateRequest(string? displayName) => new()
    {
        FullName = "Full name",
        DisplayName = displayName,
        Email = "member@vnz.vn",
        Position = "Developer",
        JobLevel = nameof(JobLevel.Junior),
        JoinedDate = DateTimeOffset.UtcNow
    };

    private static Request.UpdateTeamMemberRequest UpdateRequest(string? displayName) => new()
    {
        FullName = "Updated full name",
        DisplayName = displayName,
        Email = "member@vnz.vn",
        Position = "Developer",
        JobLevel = nameof(JobLevel.Junior),
        JoinedDate = DateTimeOffset.UtcNow,
        EmploymentStatus = nameof(EmploymentStatus.Working),
        IsPublished = false
    };
}
