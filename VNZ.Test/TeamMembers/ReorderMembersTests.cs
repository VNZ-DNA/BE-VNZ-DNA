using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.TeamMembers;
using Xunit;
using TeamMemberService = VNZ.Service.TeamMembers.Service;

namespace VNZ.Test.TeamMembers;

public class ReorderMembersTests
{
    [Fact]
    public async Task ReorderMembersAsync_UpdatesDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new TestAppDbContext(options);

        var firstMember = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            Email = "a@vnz.vn",
            PasswordHash = "password",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true,
            IsPublished = true,
            DisplayOrder = 1,
            CreateAt = DateTimeOffset.UtcNow
        };

        var secondMember = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyen Van B",
            Email = "b@vnz.vn",
            PasswordHash = "password",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true,
            IsPublished = true,
            DisplayOrder = 2,
            CreateAt = DateTimeOffset.UtcNow
        };

        dbContext.Users.AddRange(firstMember, secondMember);
        await dbContext.SaveChangesAsync();

        var service = new TeamMemberService(dbContext);

        var response = await service.ReorderMembersAsync(
            new Request.ReorderTeamMembersRequest
            {
                OrderedMemberIds = new List<Guid> { secondMember.Id, firstMember.Id }
            });

        Assert.Equal(2, response.Count);
        Assert.Equal(secondMember.Id, response[0].Id);
        Assert.Equal(1, response[0].DisplayOrder);
        Assert.Equal(firstMember.Id, response[1].Id);
        Assert.Equal(2, response[1].DisplayOrder);

        var savedFirstMember = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(member => member.Id == firstMember.Id);
        var savedSecondMember = await dbContext.Users
            .AsNoTracking()
            .SingleAsync(member => member.Id == secondMember.Id);

        Assert.Equal(2, savedFirstMember.DisplayOrder);
        Assert.Equal(1, savedSecondMember.DisplayOrder);
        Assert.NotNull(savedFirstMember.UpdatedAt);
        Assert.NotNull(savedSecondMember.UpdatedAt);
    }
}
