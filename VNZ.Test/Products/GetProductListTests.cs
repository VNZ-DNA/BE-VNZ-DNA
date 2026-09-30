using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.ProductService;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class GetProductListTests
{
    [Fact]
    public async Task GetProductListAsync_ReturnsPublishedProductsFirstAndDescriptionSummary()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ProductListTestDbContext(options);
        var publishedFirst = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Portal",
            LogoUrl = "portal.svg",
            ProductUrl = "https://portal.vnz.example",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = new DateTimeOffset(2026, 9, 20, 8, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero),
            Content = new ProductContent
            {
                Blocks = new List<ContentBlock>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Title,
                        Order = 1,
                        Text = "VNZ Portal"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Description,
                        Order = 2,
                        Text = "Portal summary"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Description,
                        Order = 3,
                        Text = "Later description"
                    }
                }
            }
        };

        var publishedSecond = CreateProduct(
            "VNZ Academy",
            ProductStatus.Completed,
            isPublished: true,
            displayOrder: 2,
            createdAt: new DateTimeOffset(2026, 9, 19, 8, 0, 0, TimeSpan.Zero));
        var unpublishedNewest = CreateProduct(
            "VNZ DNA",
            ProductStatus.InProgress,
            isPublished: false,
            displayOrder: null,
            createdAt: new DateTimeOffset(2026, 9, 25, 8, 0, 0, TimeSpan.Zero));
        var unpublishedOlder = CreateProduct(
            "VNZ Analytics",
            ProductStatus.Completed,
            isPublished: false,
            displayOrder: null,
            createdAt: new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero));

        dbContext.Products.AddRange(publishedFirst, publishedSecond, unpublishedNewest, unpublishedOlder);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.GetProductListAsync(new Request.GetProductListRequest());

        Assert.Equal(4, response.Total);
        Assert.Equal(1, response.TotalPages);
        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(4, response.Items.Count);

        Assert.Equal(publishedFirst.Id, response.Items[0].Id);
        Assert.Equal("VNZ Portal", response.Items[0].Name);
        Assert.Equal("portal.svg", response.Items[0].LogoUrl);
        Assert.Equal("https://portal.vnz.example", response.Items[0].ProductUrl);
        Assert.Equal("Portal summary", response.Items[0].Summary);
        Assert.Equal(ProductStatus.Completed, response.Items[0].Status);
        Assert.True(response.Items[0].IsPublished);
        Assert.Equal(1, response.Items[0].DisplayOrder);
        Assert.Equal(publishedFirst.UpdatedAt, response.Items[0].UpdatedAt);

        Assert.Equal(publishedSecond.Id, response.Items[1].Id);
        Assert.Equal(unpublishedNewest.Id, response.Items[2].Id);
        Assert.Equal(ProductStatus.InProgress, response.Items[2].Status);
        Assert.False(response.Items[2].IsPublished);
        Assert.Null(response.Items[2].DisplayOrder);
        Assert.Equal(unpublishedOlder.Id, response.Items[3].Id);
    }

    private static Product CreateProduct(
        string name,
        ProductStatus status,
        bool isPublished,
        int? displayOrder,
        DateTimeOffset createdAt)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = status,
            IsPublished = isPublished,
            DisplayOrder = displayOrder,
            CreatedAt = createdAt
        };
    }

    private sealed class ProductListTestDbContext : AppDbContext
    {
        public ProductListTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPost>().Ignore(jobPost => jobPost.Skills);
            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Images);

            var productContentConverter = new ValueConverter<ProductContent?, string?>(
                content => JsonSerializer.Serialize(content, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ProductContent>(json!, (JsonSerializerOptions?)null));

            modelBuilder.Entity<Product>()
                .Property(product => product.Content)
                .HasConversion(productContentConverter);
        }
    }
}
