using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class GetOrderableProductsTests
{
    [Fact]
    public async Task GetOrderableProductsAsync_ReturnsPublishedProductsInDisplayOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);

        var secondProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Academy",
            LogoUrl = "academy.svg",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 2,
            CreatedAt = new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero)
        };
        var unpublishedProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ DNA",
            Status = ProductStatus.InProgress,
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = new DateTimeOffset(2026, 9, 25, 8, 0, 0, TimeSpan.Zero)
        };
        var firstProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Portal",
            LogoUrl = "portal.svg",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        };

        dbContext.Products.AddRange(secondProduct, unpublishedProduct, firstProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(dbContext);

        var response = await service.GetOrderableProductsAsync();

        Assert.Equal(2, response.Count);
        Assert.Equal(firstProduct.Id, response[0].Id);
        Assert.Equal("VNZ Portal", response[0].Name);
        Assert.Equal("portal.svg", response[0].LogoUrl);
        Assert.Equal(1, response[0].DisplayOrder);

        Assert.Equal(secondProduct.Id, response[1].Id);
        Assert.Equal("VNZ Academy", response[1].Name);
        Assert.Equal("academy.svg", response[1].LogoUrl);
        Assert.Equal(2, response[1].DisplayOrder);
    }
}
