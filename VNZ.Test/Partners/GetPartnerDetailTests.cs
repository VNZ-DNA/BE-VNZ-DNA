using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class GetPartnerDetailTests
{
    [Fact]
    public async Task GetPartnerDetailAsync_ReturnsFullDetailsForUnpublishedPartner()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var createdBy = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        var creator = new User
        {
            Id = createdBy,
            FullName = "Partner Creator",
            Email = "creator-" + Guid.NewGuid().ToString("N") + "@vnz.vn",
            PasswordHash = "password-hash",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true
        };
        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            Name = "VNZ Technology Partner",
            LogoUrl = "https://partner.vn/logo.png",
            WebsiteUrl = "https://partner.vn",
            Description = "Technology solutions partner",
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = createdAt,
            UpdateAt = updatedAt
        };

        dbContext.Users.Add(creator);
        dbContext.Partners.Add(partner);
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(
            dbContext,
            new VNZ.Test.TestMediaService());

        var response = await service.GetPartnerDetailAsync(partner.Id);

        Assert.Equal(partner.Id, response.Id);
        Assert.Equal("VNZ Technology Partner", response.Name);
        Assert.Equal("https://partner.vn/logo.png", response.LogoUrl);
        Assert.Equal("https://partner.vn", response.WebsiteUrl);
        Assert.Equal("Technology solutions partner", response.Description);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Equal(createdBy, response.CreatedBy);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);
    }
}
