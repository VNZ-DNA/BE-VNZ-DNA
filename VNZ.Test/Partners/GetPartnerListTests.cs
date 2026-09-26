using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Service.PartnerService;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class GetPartnerListTests
{
    [Fact]
    public async Task GetPartnerListAsync_SearchesWithoutCaseSensitivityAndReturnsPagedOrderedResults()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var firstPublishedPartner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "VNZ partner Beta",
            LogoUrl = "https://beta.vn/logo.png",
            WebsiteUrl = "https://beta.vn",
            Description = "Beta description",
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = new DateTimeOffset(2026, 9, 2, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 3, 8, 0, 0, TimeSpan.Zero)
        };
        var secondPublishedPartner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Partner Alpha",
            IsPublished = true,
            DisplayOrder = 2,
            CreatedAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero)
        };
        var unpublishedPartner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "VNZ PARTNER Gamma",
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 4, 8, 0, 0, TimeSpan.Zero)
        };
        var nonMatchingPartner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "Other Company",
            IsPublished = true,
            DisplayOrder = 0,
            CreatedAt = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero),
            UpdateAt = new DateTimeOffset(2026, 9, 5, 8, 0, 0, TimeSpan.Zero)
        };
        dbContext.Partners.AddRange(
            secondPublishedPartner,
            unpublishedPartner,
            nonMatchingPartner,
            firstPublishedPartner);
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(dbContext);

        var firstPage = await service.GetPartnerListAsync(
            new Request.GetPartnerListRequest
            {
                Search = "  vnz PARTNER  ",
                Page = 1,
                PageSize = 2
            });
        var secondPage = await service.GetPartnerListAsync(
            new Request.GetPartnerListRequest
            {
                Search = "  vnz PARTNER  ",
                Page = 2,
                PageSize = 2
            });

        Assert.Equal(3, firstPage.Total);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, firstPage.PageSize);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(firstPublishedPartner.Id, firstPage.Items[0].Id);
        Assert.Equal("VNZ partner Beta", firstPage.Items[0].Name);
        Assert.Equal("https://beta.vn/logo.png", firstPage.Items[0].LogoUrl);
        Assert.Equal("https://beta.vn", firstPage.Items[0].WebsiteUrl);
        Assert.Equal("Beta description", firstPage.Items[0].Description);
        Assert.True(firstPage.Items[0].IsPublished);
        Assert.Equal(1, firstPage.Items[0].DisplayOrder);
        Assert.Equal(firstPublishedPartner.CreatedAt, firstPage.Items[0].CreatedAt);
        Assert.Equal(firstPublishedPartner.UpdateAt, firstPage.Items[0].UpdatedAt);
        Assert.Equal(secondPublishedPartner.Id, firstPage.Items[1].Id);

        Assert.Equal(2, secondPage.Page);
        Assert.Single(secondPage.Items);
        Assert.Equal(unpublishedPartner.Id, secondPage.Items[0].Id);
        Assert.False(secondPage.Items[0].IsPublished);
        Assert.Null(secondPage.Items[0].DisplayOrder);
    }
}
