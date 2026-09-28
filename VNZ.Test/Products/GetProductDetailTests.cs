using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using Xunit;
using ProductService = VNZ.Service.ProductService.Service;

namespace VNZ.Test.Products;

public class GetProductDetailTests
{
    [Fact]
    public async Task GetProductDetailAsync_ReturnsProductAndContentBlocksInOrder()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ProductDetailTestDbContext(options);
        var productId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var createdAt = new DateTimeOffset(2026, 9, 1, 2, 30, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 9, 14, 4, 20, 0, TimeSpan.Zero);
        var featureItemId = Guid.NewGuid();
        var creator = new User
        {
            Id = createdBy,
            FullName = "Product Creator",
            Email = "creator@vnz.vn",
            PasswordHash = "password-hash",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true
        };

        dbContext.Users.Add(creator);
        dbContext.Products.Add(new Product
        {
            Id = productId,
            Name = "VNZ Analytics 2.0",
            LogoUrl = "analytics-logo.png",
            ProductUrl = "https://vnz.vn/products/analytics",
            Content = new ProductContent
            {
                Blocks = new List<ContentBlock>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Feature,
                        Order = 3,
                        Items = new List<FeatureItem>
                        {
                            new() { Id = featureItemId, Title = "Real-time reports" }
                        }
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Title,
                        Order = 2,
                        Text = "VNZ Analytics 2.0"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Description,
                        Order = 1,
                        Text = "Analytics product description"
                    }
                }
            },
            Status = ProductStatus.InProgress,
            IsPublished = false,
            DisplayOrder = null,
            CreatedBy = createdBy,
            Creator = creator,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        });
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.GetProductDetailAsync(productId);

        Assert.Equal(productId, response.Id);
        Assert.Equal("VNZ Analytics 2.0", response.Name);
        Assert.Equal("analytics-logo.png", response.LogoUrl);
        Assert.Equal("https://vnz.vn/products/analytics", response.ProductUrl);
        Assert.Equal(ProductStatus.InProgress, response.Status);
        Assert.False(response.IsPublished);
        Assert.Null(response.DisplayOrder);
        Assert.Equal(createdBy, response.CreatedBy);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(updatedAt, response.UpdatedAt);

        Assert.NotNull(response.Content);
        Assert.Equal(3, response.Content.Blocks.Count);
        Assert.Equal(new[] { 1, 2, 3 }, response.Content.Blocks.Select(block => block.Order));
        Assert.Equal(ContentBlockType.Description, response.Content.Blocks[0].Type);
        Assert.Equal("Analytics product description", response.Content.Blocks[0].Text);
        Assert.Equal(ContentBlockType.Title, response.Content.Blocks[1].Type);
        Assert.Equal(ContentBlockType.Feature, response.Content.Blocks[2].Type);
        Assert.Equal(featureItemId, Assert.Single(response.Content.Blocks[2].Items!).Id);
        Assert.Equal("Real-time reports", response.Content.Blocks[2].Items![0].Title);
    }

    private sealed class ProductDetailTestDbContext : AppDbContext
    {
        public ProductDetailTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPost>().Ignore(jobPost => jobPost.Skills);
            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);

            var productContentConverter = new ValueConverter<ProductContent?, string?>(
                content => JsonSerializer.Serialize(content, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ProductContent>(json!, (JsonSerializerOptions?)null));

            modelBuilder.Entity<Product>()
                .Property(product => product.Content)
                .HasConversion(productContentConverter);
        }
    }
}
