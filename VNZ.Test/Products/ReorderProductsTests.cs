using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.ProductService;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class ReorderProductsTests
{
    [Fact]
    public async Task ReorderProductsAsync_UpdatesPublishedOrderAndLeavesUnpublishedProductUnchanged()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var dbContext = new VNZ.Test.TeamMembers.TestAppDbContext(options);

        var firstProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Portal",
            LogoUrl = "portal.svg",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var secondProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Academy",
            LogoUrl = "academy.svg",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 2,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var unpublishedProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ DNA",
            Status = ProductStatus.InProgress,
            IsPublished = false,
            DisplayOrder = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Products.AddRange(firstProduct, secondProduct, unpublishedProduct);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.ReorderProductsAsync(
            new Request.ReorderProductsRequest
            {
                OrderedProductIds = new List<Guid> { secondProduct.Id, firstProduct.Id }
            });

        Assert.Equal(2, response.Count);
        Assert.Equal(secondProduct.Id, response[0].Id);
        Assert.Equal("VNZ Academy", response[0].Name);
        Assert.Equal("academy.svg", response[0].LogoUrl);
        Assert.Equal(1, response[0].DisplayOrder);
        Assert.Equal(firstProduct.Id, response[1].Id);
        Assert.Equal("VNZ Portal", response[1].Name);
        Assert.Equal("portal.svg", response[1].LogoUrl);
        Assert.Equal(2, response[1].DisplayOrder);

        var savedFirstProduct = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == firstProduct.Id);
        var savedSecondProduct = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == secondProduct.Id);
        var savedUnpublishedProduct = await dbContext.Products
            .AsNoTracking()
            .SingleAsync(product => product.Id == unpublishedProduct.Id);

        Assert.Equal(2, savedFirstProduct.DisplayOrder);
        Assert.Equal(1, savedSecondProduct.DisplayOrder);
        Assert.NotNull(savedFirstProduct.UpdatedAt);
        Assert.NotNull(savedSecondProduct.UpdatedAt);
        Assert.False(savedUnpublishedProduct.IsPublished);
        Assert.Null(savedUnpublishedProduct.DisplayOrder);
        Assert.Null(savedUnpublishedProduct.UpdatedAt);
    }
}
