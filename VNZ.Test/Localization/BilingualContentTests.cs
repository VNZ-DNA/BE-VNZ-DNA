using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;
using VNZ.Service.JobPostService;
using VNZ.Service.Localization;
using VNZ.Service.NewsService;
using VNZ.Service.ProductService;
using Xunit;
using JobPostService = VNZ.Service.JobPostService.Service;
using NewsService = VNZ.Service.NewsService.Service;
using ProductService = VNZ.Service.ProductService.Service;
using NewsRequest = VNZ.Service.NewsService.Request;
using ProductRequest = VNZ.Service.ProductService.Request;

namespace VNZ.Test.Localization;

public sealed class BilingualContentTests
{
    [Fact]
    public void LocaleResolver_RequiresOneExactSupportedLocale()
    {
        Assert.Equal(LocaleResolver.Vietnamese, LocaleResolver.Resolve((string?)null));
        Assert.Equal(LocaleResolver.English, LocaleResolver.Resolve("en"));

        var invalid = Assert.Throws<LocalizationException>(() => LocaleResolver.Resolve("EN"));
        Assert.Equal("LANGUAGE_NOT_SUPPORTED", invalid.Code);

        var multiple = Assert.Throws<LocalizationException>(() =>
            LocaleResolver.Resolve(new string?[] { "vi", "en" }));
        Assert.Equal("LANGUAGE_NOT_SUPPORTED", multiple.Code);
    }

    [Fact]
    public async Task CreateNewsDraft_AllowsPartialVietnameseAndEnglish()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();

        var service = new NewsService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.CreateNewsAsync(
            new NewsRequest.CreateNewsRequest
            {
                Status = nameof(NewsStatus.Draft),
                Translations = new NewsRequest.NewsTranslationsRequest
                {
                    En = new NewsRequest.NewsEnglishTranslationRequest
                    {
                        Title = "English draft title"
                    }
                }
            },
            admin.Id);

        var saved = await dbContext.NewsArticles
            .AsNoTracking()
            .SingleAsync(article => article.Id == response.Id);

        Assert.Null(saved.Title);
        Assert.Equal("English draft title", saved.Translations!.En!.Title);
        Assert.Equal(NewsStatus.Draft, saved.Status);
        Assert.Equal("Bản nháp", response.Status);
        Assert.False(saved.Published);
    }

    [Fact]
    public async Task AdminNews_ReturnsCategoryNamesAcrossResponses()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        var technology = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Code = "Z_TECHNOLOGY",
            Name = "Công nghệ",
            CreateAt = DateTimeOffset.UtcNow
        };
        var product = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Code = "A_PRODUCT",
            Name = "Sản phẩm",
            CreateAt = DateTimeOffset.UtcNow
        };
        dbContext.Users.Add(admin);
        dbContext.NewsCategories.AddRange(product, technology);
        await dbContext.SaveChangesAsync();

        var service = new NewsService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());
        var categoryIds = new List<Guid> { product.Id, technology.Id };
        var expectedNames = new[] { "Công nghệ", "Sản phẩm" };

        var created = await service.CreateNewsAsync(new NewsRequest.CreateNewsRequest
        {
            Title = "Bài viết thử nghiệm",
            Status = nameof(NewsStatus.Draft),
            CategoryIds = categoryIds
        }, admin.Id);
        var updated = await service.UpdateNewsAsync(created.Id, new NewsRequest.UpdateNewsRequest
        {
            Title = "Bài viết thử nghiệm đã chỉnh sửa",
            Status = nameof(NewsStatus.Draft),
            CategoryIds = categoryIds
        });
        var list = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            CategoryId = [technology.Id.ToString()]
        });
        var detail = await service.GetNewsDetailAsync(created.Id);
        var categories = await service.GetNewsCategoriesAsync();

        Assert.Equal(expectedNames, created.Categories.Select(category => category.Name));
        Assert.Equal(expectedNames, updated.Categories.Select(category => category.Name));
        Assert.Equal(expectedNames, Assert.Single(list.Items).Categories.Select(category => category.Name));
        Assert.Equal(expectedNames, detail.Categories.Select(category => category.Name));
        Assert.Equal(expectedNames, categories.Select(category => category.Name));
    }

    [Fact]
    public async Task CreateJobPostDraft_AllowsMissingVietnameseTitle()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();

        var response = await new JobPostService(dbContext).CreateJobPostAsync(
            new VNZ.Service.JobPostService.Request.CreateJobPostRequest
            {
                Action = JobPostAction.SavedDraft,
                Translations = new VNZ.Service.JobPostService.Request.JobPostTranslationsRequest
                {
                    En = new VNZ.Service.JobPostService.Request.JobPostEnglishTranslationRequest
                    {
                        Title = "English draft"
                    }
                }
            },
            admin.Id);

        var saved = await dbContext.JobPosts.AsNoTracking().SingleAsync(post => post.Id == response.Id);
        Assert.Null(saved.Title);
        Assert.Equal(JobPostStatus.Draft, saved.Status);
        Assert.Equal("Bản nháp", response.Status);
        Assert.Equal("English draft", saved.Translations!.En!.Title);
    }

    [Fact]
    public async Task CreateNewsPublishWithoutEnglish_ReturnsBilingualRequiredAndDoesNotWrite()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync();

        var service = new NewsService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var exception = await Assert.ThrowsAsync<NewsException>(() => service.CreateNewsAsync(
            new NewsRequest.CreateNewsRequest
            {
                Status = nameof(NewsStatus.Published),
                Title = "Vietnamese title",
                Summary = "<p>Vietnamese summary</p>",
                Content = "<p>" + new string('v', 300) + "</p>"
            },
            admin.Id));

        Assert.Equal("BILINGUAL_CONTENT_REQUIRED", exception.Code);
        Assert.Empty(await dbContext.NewsArticles.ToListAsync());
    }

    [Fact]
    public async Task GetPublicNewsDetail_EnglishResolvesTranslationAndStableCategoryCode()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        var category = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Code = "PRODUCT",
            Name = "Sản phẩm",
            CreateAt = DateTimeOffset.UtcNow
        };
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = "Tiêu đề Việt",
            Summary = "<p>Tóm tắt Việt</p>",
            Content = "<p>Nội dung Việt</p>",
            Status = NewsStatus.Published,
            Published = true,
            CreatedBy = admin.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            PublishAt = DateTimeOffset.UtcNow,
            Translations = new NewsTranslations
            {
                En = new NewsEnglishTranslation
                {
                    Title = "English title",
                    Summary = "<p>English summary</p>",
                    Content = "<p>English content</p>"
                }
            }
        };
        article.NewsArticleCategories.Add(new NewsArticleCategory
        {
            Id = Guid.NewGuid(),
            NewsArticle = article,
            NewsCategory = category
        });
        dbContext.Users.Add(admin);
        dbContext.NewsCategories.Add(category);
        dbContext.NewsArticles.Add(article);
        await dbContext.SaveChangesAsync();

        var service = new NewsService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var response = await service.GetPublicNewsDetailAsync(article.Id.ToString(), "en");

        Assert.Equal("English title", response.Title);
        Assert.Equal("<p>English summary</p>", response.Summary);
        Assert.Equal("<p>English content</p>", response.Content);
        Assert.Equal("PRODUCT", response.Categories.Single().Name);
    }

    [Fact]
    public async Task GetPublicJobPostList_EnglishUsesStableEnumKeysAndDepartmentCode()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "ENGINEERING",
            Name = "Kỹ thuật",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Department = department,
            DepartmentId = department.Id,
            Status = JobPostStatus.Open,
            ExpiredAt = DateTimeOffset.UtcNow.AddDays(10),
            CreatedAt = DateTimeOffset.UtcNow,
            Title = "Tiêu đề Việt",
            EmploymentType = EmploymentType.FullTime,
            JobLevel = JobLevel.Senior,
            NumberOfPositions = 1,
            Skills = new List<string> { ".NET" },
            ShortDescription = "Mô tả ngắn Việt",
            Description = "Mô tả Việt",
            Requirements = "Yêu cầu Việt",
            Translations = new JobPostTranslations
            {
                En = new JobPostEnglishTranslation
                {
                    Title = "English job title",
                    ShortDescription = "English short description",
                    Description = "English description",
                    Requirements = "English requirements"
                }
            }
        };
        dbContext.Departments.Add(department);
        dbContext.JobPosts.Add(jobPost);
        await dbContext.SaveChangesAsync();

        var response = await new JobPostService(dbContext)
            .GetPublicJobPostListAsync("en");

        var item = response.Single();
        Assert.Equal("English job title", item.Title);
        Assert.Equal("ENGINEERING", item.Department);
        Assert.Equal("FullTime", item.EmploymentType);
        Assert.Equal("Senior", item.JobLevel);
        Assert.Equal("English short description", item.ShortDescription);
    }

    [Fact]
    public async Task GetPublicProductList_EnglishResolvesOnlyPublicContentFields()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var titleId = Guid.NewGuid();
        var featureBlockId = Guid.NewGuid();
        var featureItemId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Internal product name",
            LogoUrl = "logo.svg",
            WordmarkUrl = "wordmark.svg",
            ProductUrl = "https://example.test/product",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Content = new ProductContent
            {
                Blocks = new List<ContentBlock>
                {
                    new()
                    {
                        Id = titleId,
                        Type = ContentBlockType.Title,
                        Order = 1,
                        Text = "Tiêu đề Việt"
                    },
                    new()
                    {
                        Id = featureBlockId,
                        Type = ContentBlockType.Feature,
                        Order = 2,
                        Items = new List<FeatureItem>
                        {
                            new() { Id = featureItemId, Title = "Tính năng Việt" }
                        }
                    }
                }
            },
            Translations = new ProductTranslations
            {
                En = new ProductEnglishTranslation
                {
                    Content = new ProductContent
                    {
                        Blocks = new List<ContentBlock>
                        {
                            new()
                            {
                                Id = titleId,
                                Type = ContentBlockType.Title,
                                Order = 1,
                                Text = "English title"
                            },
                            new()
                            {
                                Id = featureBlockId,
                                Type = ContentBlockType.Feature,
                                Order = 2,
                                Items = new List<FeatureItem>
                                {
                                    new() { Id = featureItemId, Title = "English feature" }
                                }
                            }
                        }
                    }
                }
            }
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var response = await new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service())
            .GetPublicProductListAsync("en");

        var item = response.Items.Single();
        Assert.Equal("English title", item.Content!.Blocks[0].Text);
        Assert.Equal("English feature", item.Content.Blocks[1].Items!.Single().Title);
        Assert.Equal("logo.svg", item.LogoUrl);
        Assert.Equal("wordmark.svg", item.WordmarkUrl);
        Assert.Null(item.GetType().GetProperty("Name"));
    }

    [Fact]
    public async Task PublishProductWithoutEnglishContent_IsRejectedBeforeWrite()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var blockId = Guid.NewGuid();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Product",
            LogoUrl = "logo.svg",
            WordmarkUrl = "wordmark.svg",
            Status = ProductStatus.Completed,
            IsPublished = false,
            CreatedAt = DateTimeOffset.UtcNow,
            Content = null
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var service = new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service());

        var exception = await Assert.ThrowsAsync<ProductException>(() => service.UpdateProductAsync(
            product.Id,
            new ProductRequest.UpdateProductRequest
            {
                Name = product.Name,
                Status = ProductStatus.Completed,
                IsPublished = true,
                Content = new ProductRequest.ProductContentRequest
                {
                    Blocks = new List<ProductRequest.ContentBlockRequest>
                    {
                        new()
                        {
                            Id = blockId,
                            Type = ContentBlockType.Title,
                            Order = 1,
                            Text = "Vietnamese title"
                        }
                    }
                }
            }));

        Assert.Equal("BILINGUAL_CONTENT_REQUIRED", exception.Code);
        Assert.False((await dbContext.Products.SingleAsync()).IsPublished);
    }

    [Fact]
    public async Task ClosePublishedNews_IsStateOnlyAndPreservesBilingualContent()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var admin = CreateAdmin();
        var category = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Code = "PRODUCT",
            Name = "Sản phẩm",
            CreateAt = DateTimeOffset.UtcNow
        };
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = "VI title",
            Summary = "<p>VI summary</p>",
            Content = "<p>VI content</p>",
            Status = NewsStatus.Published,
            Published = true,
            CreatedBy = admin.Id,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            PublishAt = DateTimeOffset.UtcNow.AddDays(-1),
            Translations = new NewsTranslations
            {
                En = new NewsEnglishTranslation
                {
                    Title = "EN title",
                    Summary = "<p>EN summary</p>",
                    Content = "<p>EN content</p>"
                }
            }
        };
        article.NewsArticleCategories.Add(new NewsArticleCategory
        {
            Id = Guid.NewGuid(),
            NewsArticle = article,
            NewsCategory = category
        });
        dbContext.Users.Add(admin);
        dbContext.NewsCategories.Add(category);
        dbContext.NewsArticles.Add(article);
        await dbContext.SaveChangesAsync();

        var response = await new NewsService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service())
            .UpdateNewsAsync(
                article.Id,
                new NewsRequest.UpdateNewsRequest
                {
                    Status = nameof(NewsStatus.Closed)
                });

        var saved = await dbContext.NewsArticles.AsNoTracking().SingleAsync();

        Assert.Equal("Đã đóng", response.Status);
        Assert.Equal("Sản phẩm", Assert.Single(response.Categories).Name);
        Assert.False(saved.Published);
        Assert.Equal(article.Title, saved.Title);
        Assert.Equal(article.Content, saved.Content);
        Assert.Equal(article.PublishAt, saved.PublishAt);
        Assert.Equal("EN title", saved.Translations!.En!.Title);
    }

    [Fact]
    public async Task PublishJobPost_AllowsEmptySkillsAndNormalizesToEmptyArray()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "ENGINEERING",
            Name = "Kỹ thuật",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();

        var response = await new JobPostService(dbContext).CreateJobPostAsync(
            new VNZ.Service.JobPostService.Request.CreateJobPostRequest
            {
                Action = JobPostAction.Publish,
                DepartmentId = department.Id,
                EmploymentType = EmploymentType.FullTime,
                JobLevel = JobLevel.Junior,
                NumberOfPositions = 1,
                ExpiredDate = new DateOnly(2030, 12, 31),
                Title = "VI title",
                ShortDescription = "VI short",
                Description = "VI description",
                Requirements = "VI requirements",
                Skills = new List<string>(),
                Translations = new VNZ.Service.JobPostService.Request.JobPostTranslationsRequest
                {
                    En = new VNZ.Service.JobPostService.Request.JobPostEnglishTranslationRequest
                    {
                        Title = "EN title",
                        ShortDescription = "EN short",
                        Description = "EN description",
                        Requirements = "EN requirements"
                    }
                }
            },
            Guid.NewGuid());

        var saved = await dbContext.JobPosts.AsNoTracking().SingleAsync();

        Assert.Equal("Đang tuyển", response.Status);
        Assert.Empty(saved.Skills);
    }

    [Fact]
    public async Task CloseJobPost_ReturnsStatusAndEmploymentTypeDisplayNames()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            Title = "Backend role",
            Status = JobPostStatus.Open,
            EmploymentType = EmploymentType.FullTime,
            JobLevel = JobLevel.Junior,
            ExpiredAt = DateTimeOffset.UtcNow.AddDays(30),
            Skills = new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.JobPosts.Add(jobPost);
        await dbContext.SaveChangesAsync();

        var response = await new JobPostService(dbContext).UpdateJobPostAsync(
            jobPost.Id,
            new VNZ.Service.JobPostService.Request.UpdateJobPostRequest
            {
                Action = JobPostAction.Close
            });

        Assert.Equal("Đã đóng", response.Status);
        Assert.Equal("Toàn thời gian", response.EmploymentType);
        Assert.Equal("Junior", response.JobLevel);
        Assert.Equal(JobPostStatus.Closed, (await dbContext.JobPosts.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task UnpublishProduct_IsStateOnlyAndPreservesContent()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "VNZ Product",
            LogoUrl = "logo.svg",
            WordmarkUrl = "wordmark.svg",
            Status = ProductStatus.Completed,
            IsPublished = true,
            DisplayOrder = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            Content = new ProductContent
            {
                Blocks = new List<ContentBlock>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Type = ContentBlockType.Title,
                        Order = 1,
                        Text = "VI title"
                    }
                }
            },
            Translations = new ProductTranslations
            {
                En = new ProductEnglishTranslation
                {
                    Content = new ProductContent
                    {
                        Blocks = new List<ContentBlock>
                        {
                            new()
                            {
                                Id = Guid.NewGuid(),
                                Type = ContentBlockType.Title,
                                Order = 1,
                                Text = "EN title"
                            }
                        }
                    }
                }
            }
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        await new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service())
            .UpdateProductAsync(
                product.Id,
                new ProductRequest.UpdateProductRequest
                {
                    IsPublished = false
                });

        var saved = await dbContext.Products.AsNoTracking().SingleAsync();

        Assert.False(saved.IsPublished);
        Assert.Null(saved.DisplayOrder);
        Assert.Equal("VI title", saved.Content!.Blocks.Single().Text);
        Assert.Equal("EN title", saved.Translations!.En!.Content!.Blocks.Single().Text);
    }

    [Fact]
    public async Task ProductFeatureWithoutMultipartItems_IsNormalizedToEmptyArray()
    {
        var options = CreateOptions();
        await using var dbContext = new BilingualTestDbContext(options);
        var blockId = Guid.NewGuid();

        var response = await new ProductService(
            dbContext,
            new VNZ.Test.TestMediaService(),
            new VNZ.Service.Utils.RichTextService.Service())
            .CreateProductAsync(
                new ProductRequest.CreateProductRequest
                {
                    Name = "VNZ Product",
                    Content = new ProductRequest.ProductContentRequest
                    {
                        Blocks = new List<ProductRequest.ContentBlockRequest>
                        {
                            new()
                            {
                                Id = blockId,
                                Type = ContentBlockType.Feature,
                                Order = 1,
                                Items = null
                            }
                        }
                    }
                },
                Guid.NewGuid());

        var saved = await dbContext.Products.AsNoTracking().SingleAsync(product => product.Id == response.Id);

        Assert.NotNull(saved.Content!.Blocks.Single().Items);
        Assert.Empty(saved.Content.Blocks.Single().Items!);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private static User CreateAdmin()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "Admin User",
            Email = Guid.NewGuid() + "@vnz.test",
            PasswordHash = "hash",
            EmploymentStatus = EmploymentStatus.Working,
            IsActive = true
        };
    }

    private sealed class BilingualTestDbContext : AppDbContext
    {
        public BilingualTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Images);

            var productContentConverter = new ValueConverter<ProductContent?, string?>(
                content => JsonSerializer.Serialize(content, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonSerializer.Deserialize<ProductContent>(json, (JsonSerializerOptions?)null));

            modelBuilder.Entity<Product>()
                .Property(product => product.Content)
                .HasConversion(productContentConverter);

            var skillsConverter = new ValueConverter<List<string>, string>(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!);

            modelBuilder.Entity<JobPost>()
                .Property(jobPost => jobPost.Skills)
                .HasConversion(skillsConverter);
        }
    }
}
