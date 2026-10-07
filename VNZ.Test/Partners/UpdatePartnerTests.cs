using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Service.PartnerService;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class UpdatePartnerTests
{
    [Fact]
    public async Task UpdatePartnerAsync_UpdatesUnpublishedPartnerAndKeepsItUnpublished()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var createdAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var previousUpdatedAt = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);
        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "Old Partner Name",
            LogoUrl = "https://old.vn/logo.png",
            WebsiteUrl = "https://old.vn",
            Description = "Old description",
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = createdAt,
            UpdateAt = previousUpdatedAt
        };
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.UpdatePartnerAsync(
            partner.Id,
            new Request.UpdatePartnerRequest
            {
                Name = "  VNZ Updated Partner  ",
                LogoUrl = "https://vnz.vn/logo.png",
                WebsiteUrl = "https://vnz.vn",
                Description = "Updated partner description"
            });

        Assert.Equal(partner.Id, response.Id);
        Assert.Equal("VNZ Updated Partner", response.Name);
        Assert.Equal("https://vnz.vn/logo.png", response.LogoUrl);
        Assert.Equal("https://vnz.vn", response.WebsiteUrl);
        Assert.Equal("Updated partner description", response.Description);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Null(response.CreatedBy);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.NotEqual(previousUpdatedAt, response.UpdatedAt);

        var savedPartner = await dbContext.Partners
            .AsNoTracking()
            .SingleAsync(item => item.Id == partner.Id);

        Assert.Equal("VNZ Updated Partner", savedPartner.Name);
        Assert.Equal("https://vnz.vn/logo.png", savedPartner.LogoUrl);
        Assert.Equal("https://vnz.vn", savedPartner.WebsiteUrl);
        Assert.Equal("Updated partner description", savedPartner.Description);
        Assert.False(savedPartner.IsPublished);
        Assert.Null(savedPartner.DisplayOrder);
        Assert.Equal(createdAt, savedPartner.CreatedAt);
        Assert.Equal(response.UpdatedAt, savedPartner.UpdateAt);
    }
}
