using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.PartnerService;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class ReorderPartnersTests
{
    [Fact]
    public async Task ReorderPartnersAsync_UpdatesPublishedOrderAndLeavesUnpublishedPartnerUnchanged()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var admin = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = "admin-" + Guid.NewGuid().ToString("N") + "@vnz.vn",
            PasswordHash = "password-hash",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true
        };
        dbContext.Users.Add(admin);

        var firstPartner = new Partner
        {
            Id = Guid.NewGuid(),
            CreatedBy = admin.Id,
            Name = "First Partner",
            LogoUrl = "https://first.vn/logo.png",
            WebsiteUrl = "https://first.vn",
            Description = "First description",
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero)
        };
        var secondPartner = new Partner
        {
            Id = Guid.NewGuid(),
            CreatedBy = admin.Id,
            Name = "Second Partner",
            IsPublished = true,
            DisplayOrder = 2,
            CreatedAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero)
        };
        var unpublishedPartner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "Unpublished Partner",
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero)
        };
        dbContext.Partners.AddRange(firstPartner, secondPartner, unpublishedPartner);
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(dbContext);

        var response = await service.ReorderPartnersAsync(
            new Request.ReorderPartnersRequest
            {
                OrderedPartnerIds = new List<Guid> { secondPartner.Id, firstPartner.Id }
            });

        Assert.Equal(2, response.Count);
        Assert.Equal(secondPartner.Id, response[0].Id);
        Assert.Equal("Second Partner", response[0].Name);
        Assert.True(response[0].IsPublished);
        Assert.Equal(1, response[0].DisplayOrder);
        Assert.Equal(admin.Id, response[0].CreatedBy);
        Assert.Equal(secondPartner.CreatedAt, response[0].CreatedAt);
        Assert.Equal(response[0].UpdatedAt, response[1].UpdatedAt);

        Assert.Equal(firstPartner.Id, response[1].Id);
        Assert.Equal("First Partner", response[1].Name);
        Assert.Equal("https://first.vn/logo.png", response[1].LogoUrl);
        Assert.Equal("https://first.vn", response[1].WebsiteUrl);
        Assert.Equal("First description", response[1].Description);
        Assert.True(response[1].IsPublished);
        Assert.Equal(2, response[1].DisplayOrder);
        Assert.Equal(admin.Id, response[1].CreatedBy);
        Assert.Equal(firstPartner.CreatedAt, response[1].CreatedAt);

        var savedFirstPartner = await dbContext.Partners
            .AsNoTracking()
            .SingleAsync(partner => partner.Id == firstPartner.Id);
        var savedSecondPartner = await dbContext.Partners
            .AsNoTracking()
            .SingleAsync(partner => partner.Id == secondPartner.Id);
        var savedUnpublishedPartner = await dbContext.Partners
            .AsNoTracking()
            .SingleAsync(partner => partner.Id == unpublishedPartner.Id);

        Assert.Equal(2, savedFirstPartner.DisplayOrder);
        Assert.Equal(1, savedSecondPartner.DisplayOrder);
        Assert.Equal(response[0].UpdatedAt, savedFirstPartner.UpdateAt);
        Assert.Equal(response[0].UpdatedAt, savedSecondPartner.UpdateAt);
        Assert.False(savedUnpublishedPartner.IsPublished);
        Assert.Null(savedUnpublishedPartner.DisplayOrder);
        Assert.Equal(unpublishedPartner.UpdateAt, savedUnpublishedPartner.UpdateAt);
    }
}
