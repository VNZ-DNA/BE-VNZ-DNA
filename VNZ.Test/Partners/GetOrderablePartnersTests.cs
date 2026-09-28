using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class GetOrderablePartnersTests
{
    [Fact]
    public async Task GetOrderablePartnersAsync_ReturnsPublishedPartnersInDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
        dbContext.Partners.AddRange(secondPartner, unpublishedPartner, firstPartner);
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(dbContext);

        var response = await service.GetOrderablePartnersAsync();

        Assert.Equal(2, response.Count);
        Assert.Equal(firstPartner.Id, response[0].Id);
        Assert.Equal(secondPartner.Id, response[1].Id);

        Assert.Equal("First Partner", response[0].Name);
        Assert.Equal("https://first.vn/logo.png", response[0].LogoUrl);
        Assert.Equal("https://first.vn", response[0].WebsiteUrl);
        Assert.Equal("First description", response[0].Description);
        Assert.True(response[0].IsPublished);
        Assert.Equal(1, response[0].DisplayOrder);
        Assert.Equal(admin.Id, response[0].CreatedBy);
        Assert.Equal(firstPartner.CreatedAt, response[0].CreatedAt);
        Assert.Equal(firstPartner.UpdateAt, response[0].UpdatedAt);

        Assert.True(response[1].IsPublished);
        Assert.Equal(2, response[1].DisplayOrder);
    }
}
