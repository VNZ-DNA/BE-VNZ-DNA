using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.ProductService;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class CreateProductTests
{
    [Fact]
    public async Task CreateProductAsync_CreatesUnpublishedDraftFromName()
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
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.CreateProductAsync(
            new Request.CreateProductRequest { Name = "  VNZ DNA  " },
            admin.Id);

        var savedProduct = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == response.Id);

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("VNZ DNA", response.Name);
        Assert.Null(response.LogoUrl);
        Assert.Null(response.ProductUrl);
        Assert.Equal(ProductStatus.InProgress, response.Status);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Equal(admin.Id, response.CreatedBy);
        Assert.NotEqual(default, response.CreatedAt);
        Assert.Null(response.UpdatedAt);
        Assert.Null(response.Content);

        Assert.Equal("VNZ DNA", savedProduct.Name);
        Assert.Equal(ProductStatus.InProgress, savedProduct.Status);
        Assert.False(savedProduct.IsPublished);
        Assert.Null(savedProduct.DisplayOrder);
        Assert.Equal(admin.Id, savedProduct.CreatedBy);
        Assert.Null(savedProduct.Content);
    }
}
