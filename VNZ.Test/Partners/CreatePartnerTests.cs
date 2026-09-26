using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.PartnerService;
using Xunit;
using PartnerService = VNZ.Service.PartnerService.Service;

namespace VNZ.Test.Partners;

public class CreatePartnerTests
{
    [Fact]
    public async Task CreatePartnerAsync_CreatesUnpublishedPartnerWithTrimmedName()
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
        await dbContext.SaveChangesAsync();

        var service = new PartnerService(dbContext);

        var response = await service.CreatePartnerAsync(
            new Request.CreatePartnerRequest
            {
                Name = "  VNZ Partner  ",
                LogoUrl = "https://vnz.vn/logo.png",
                WebsiteUrl = "https://vnz.vn",
                Description = "Technology partner"
            },
            admin.Id);

        var savedPartner = await dbContext.Partners
            .AsNoTracking()
            .SingleAsync(partner => partner.Id == response.Id);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("VNZ Partner", response.Name);
        Assert.Equal("https://vnz.vn/logo.png", response.LogoUrl);
        Assert.Equal("https://vnz.vn", response.WebsiteUrl);
        Assert.Equal("Technology partner", response.Description);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Equal(admin.Id, response.CreatedBy);
        Assert.NotEqual(default, response.CreatedAt);
        Assert.Equal(response.CreatedAt, response.UpdatedAt);

        Assert.Equal("VNZ Partner", savedPartner.Name);
        Assert.Equal("https://vnz.vn/logo.png", savedPartner.LogoUrl);
        Assert.Equal("https://vnz.vn", savedPartner.WebsiteUrl);
        Assert.Equal("Technology partner", savedPartner.Description);
        Assert.False(savedPartner.IsPublished);
        Assert.Null(savedPartner.DisplayOrder);
        Assert.Equal(admin.Id, savedPartner.CreatedBy);
        Assert.Equal(response.CreatedAt, savedPartner.CreatedAt);
        Assert.Equal(response.UpdatedAt, savedPartner.UpdateAt);
    }
}
