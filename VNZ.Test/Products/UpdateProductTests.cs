using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.ProductService;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class UpdateProductTests
{
    [Fact]
    public async Task UpdateProductAsync_UpdatesAndPublishesCompletedUnpublishedProduct()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);
        var existingPublishedProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Portal",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 4,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero)
        };
        var productToUpdate = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Analytics",
            Status = ProductStatus.InProgress,
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.Zero)
        };

        dbContext.Products.AddRange(existingPublishedProduct, productToUpdate);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.UpdateProductAsync(
            productToUpdate.Id,
            new Request.UpdateProductRequest
            {
                Name = "  VNZ Analytics 2.0  ",
                LogoUrl = "analytics.svg",
                ProductUrl = "https://vnz.vn/products/analytics",
                Status = ProductStatus.Completed,
                IsPublished = true
            });

        var savedProduct = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == productToUpdate.Id);

        Assert.Equal(productToUpdate.Id, response.Id);
        Assert.Equal("VNZ Analytics 2.0", response.Name);
        Assert.Equal("analytics.svg", response.LogoUrl);
        Assert.Equal("https://vnz.vn/products/analytics", response.ProductUrl);
        Assert.Equal(ProductStatus.Completed, response.Status);
        Assert.True(response.IsPublished);
        Assert.Equal(5, response.DisplayOrder);
        Assert.NotNull(response.UpdatedAt);

        Assert.Equal("VNZ Analytics 2.0", savedProduct.Name);
        Assert.Equal(ProductStatus.Completed, savedProduct.Status);
        Assert.True(savedProduct.IsPublished);
        Assert.Equal(5, savedProduct.DisplayOrder);
        Assert.NotNull(savedProduct.UpdatedAt);
        Assert.Equal(4, existingPublishedProduct.DisplayOrder);
    }
}
