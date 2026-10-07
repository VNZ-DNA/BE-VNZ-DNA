using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using Xunit;
using MediaService = VNZ.Service.Utils.MediaService;
using NewsRequest = VNZ.Service.NewsService.Request;
using NewsService = VNZ.Service.NewsService.Service;
using RichTextService = VNZ.Service.Utils.RichTextService;

namespace VNZ.Test.News;

public class GetAdminNewsDateFilterTests
{
    [Theory]
    [InlineData(NewsStatus.Draft, "Bản nháp")]
    [InlineData(NewsStatus.Published, "Đã đăng")]
    [InlineData(NewsStatus.Closed, "Đã đóng")]
    public async Task GetNewsListAsync_ReturnsStatusDisplayNameAndAcceptsEnumFilter(
        NewsStatus status,
        string expectedStatus)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var article = CreateArticle(creator, "Status label article", new DateOnly(2026, 10, 1), null);
        article.Status = status;
        dbContext.Users.Add(creator);
        dbContext.NewsArticles.Add(article);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var listResponse = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            Status = [status.ToString()]
        });
        var detailResponse = await service.GetNewsDetailAsync(article.Id);

        Assert.Equal(expectedStatus, Assert.Single(listResponse.Items).Status);
        Assert.Equal(expectedStatus, detailResponse.Status);
        Assert.Equal(status, (await dbContext.NewsArticles.AsNoTracking().SingleAsync()).Status);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task GetNewsListAsync_AppliesExistingFiltersBeforeDateSortingAndPagination(string direction)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var category = new NewsCategory
        {
            Id = Guid.NewGuid(),
            Code = "Technology",
            Name = "Technology",
            CreateAt = DateTimeOffset.UtcNow
        };
        var createdFirst = CreateArticle(
            creator,
            "Backend first",
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 6));
        var createdSecond = CreateArticle(
            creator,
            "Backend second",
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 10, 7));
        var createdLast = CreateArticle(
            creator,
            "Backend last",
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 6));
        var wrongSearch = CreateArticle(
            creator,
            "Other article",
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 7));
        var wrongStatus = CreateArticle(
            creator,
            "Backend draft",
            new DateOnly(2026, 10, 4),
            null);
        var wrongCategory = CreateArticle(
            creator,
            "Backend without category",
            new DateOnly(2026, 10, 4),
            new DateOnly(2026, 10, 6));

        dbContext.Users.Add(creator);
        dbContext.NewsCategories.Add(category);
        dbContext.NewsArticles.AddRange(
            createdSecond,
            wrongSearch,
            createdLast,
            wrongStatus,
            createdFirst,
            wrongCategory);

        foreach (var article in new[] { createdFirst, createdSecond, createdLast, wrongSearch, wrongStatus })
        {
            dbContext.NewsArticleCategories.Add(new NewsArticleCategory
            {
                Id = Guid.NewGuid(),
                NewsArticleId = article.Id,
                NewsCategoryId = category.Id
            });
        }

        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            Search = " backend ",
            Status = [nameof(NewsStatus.Published)],
            CategoryId = [category.Id.ToString()],
            CreatedAt = direction,
            Page = 2,
            PageSize = 2
        });

        var expectedArticle = direction == "asc" ? createdLast : createdFirst;

        Assert.Equal(3, response.TotalItems);
        Assert.Equal(2, response.TotalPages);
        Assert.Equal(2, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Single(response.Items);
        Assert.Equal(expectedArticle.Id, response.Items[0].Id);
        Assert.Equal(expectedArticle.CreatedAt, response.Items[0].CreatedAt);
        Assert.Equal(expectedArticle.PublishAt, response.Items[0].PublishAt);
    }

    [Fact]
    public async Task GetNewsListAsync_SortsByCreatedAndPublishedDatesWithUnpublishedArticlesLast()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var createdFirst = CreateArticle(
            creator,
            "Created first",
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 5));
        var createdSecond = CreateArticle(
            creator,
            "Created second",
            new DateOnly(2026, 10, 3),
            new DateOnly(2026, 10, 4));
        var withoutPublishedDate = CreateArticle(
            creator,
            "Without published date",
            new DateOnly(2026, 10, 2),
            null);

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(
            createdSecond,
            withoutPublishedDate,
            createdFirst);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var createdAtAscending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { CreatedAt = "asc" });
        var createdAtDescending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { CreatedAt = "desc" });
        var publishAtAscending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { PublishAt = "asc" });
        var publishAtDescending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { PublishAt = "desc" });

        Assert.Equal(
            new[] { createdFirst.Id, withoutPublishedDate.Id, createdSecond.Id },
            createdAtAscending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { createdSecond.Id, withoutPublishedDate.Id, createdFirst.Id },
            createdAtDescending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { createdSecond.Id, createdFirst.Id, withoutPublishedDate.Id },
            publishAtAscending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { createdFirst.Id, createdSecond.Id, withoutPublishedDate.Id },
            publishAtDescending.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("asc", "asc", "Older,Published first,Published last,Unpublished")]
    [InlineData("asc", "desc", "Older,Published last,Published first,Unpublished")]
    [InlineData("desc", "asc", "Published first,Published last,Unpublished,Older")]
    [InlineData("desc", "desc", "Published last,Published first,Unpublished,Older")]
    [InlineData(" ASC ", " DESC ", "Older,Published last,Published first,Unpublished")]
    public async Task GetNewsListAsync_UsesCreatedDateBeforePublishedDate(
        string createdAt,
        string publishAt,
        string expectedTitles)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var older = CreateArticle(
            creator, "Older", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7));
        var publishedFirst = CreateArticle(
            creator, "Published first", new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 4));
        var publishedLast = CreateArticle(
            creator, "Published last", new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 5));
        var unpublished = CreateArticle(
            creator, "Unpublished", new DateOnly(2026, 10, 3), null);

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(unpublished, older, publishedLast, publishedFirst);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var response = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            CreatedAt = createdAt,
            PublishAt = publishAt
        });

        Assert.Equal(4, response.TotalItems);
        Assert.Equal(expectedTitles.Split(','), response.Items.Select(item => item.Title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task GetNewsListAsync_KeepsDefaultSortingWhenDateQueriesAreEmpty(string? value)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var older = CreateArticle(
            creator, "Older", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7));
        var newer = CreateArticle(
            creator, "Newer", new DateOnly(2026, 10, 3), null);

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(older, newer);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var response = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            CreatedAt = value,
            PublishAt = value
        });

        Assert.Equal(2, response.TotalItems);
        Assert.Equal(new[] { newer.Id, older.Id }, response.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("2026-10-05", "createdAt")]
    [InlineData("2026-10-06", "publishAt")]
    [InlineData("05/10/2026", "createdAt")]
    [InlineData("2026-13-01", "publishAt")]
    [InlineData("ascending", "createdAt")]
    [InlineData("descending", "publishAt")]
    public async Task GetNewsListAsync_RejectsUnsupportedDateSortValues(string value, string field)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var service = CreateService(dbContext);
        var request = new NewsRequest.GetNewsListRequest();

        if (field == "createdAt")
        {
            request.CreatedAt = value;
        }
        else
        {
            request.PublishAt = value;
        }

        var exception = await Assert.ThrowsAsync<NewsException>(
            () => service.GetNewsListAsync(request));

        Assert.Equal("NEWS_QUERY_INVALID", exception.Code);
        Assert.Equal([field], exception.Fields);
    }

    [Theory]
    [InlineData("asc", "createdAt", "Earlier,Middle,Later")]
    [InlineData("desc", "createdAt", "Later,Middle,Earlier")]
    [InlineData("asc", "publishAt", "Earlier,Middle,Later")]
    [InlineData("desc", "publishAt", "Later,Middle,Earlier")]
    public async Task GetNewsListAsync_SortsByFullTimestampsWithinTheSameCalendarDay(
        string direction,
        string field,
        string expectedTitles)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var date = new DateOnly(2026, 10, 5);
        var earlier = CreateArticle(creator, "Earlier", date, date);
        var middle = CreateArticle(creator, "Middle", date, date);
        var later = CreateArticle(creator, "Later", date, date);
        earlier.Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        middle.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        later.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        middle.CreatedAt = earlier.CreatedAt.AddMilliseconds(1);
        later.CreatedAt = earlier.CreatedAt.AddMilliseconds(2);
        middle.PublishAt = earlier.PublishAt!.Value.AddMilliseconds(1);
        later.PublishAt = earlier.PublishAt.Value.AddMilliseconds(2);

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(middle, later, earlier);
        await dbContext.SaveChangesAsync();

        var request = new NewsRequest.GetNewsListRequest();
        if (field == "createdAt")
        {
            request.CreatedAt = direction;
        }
        else
        {
            request.PublishAt = direction;
        }

        var service = CreateService(dbContext);
        var response = await service.GetNewsListAsync(request);

        Assert.Equal(expectedTitles.Split(','), response.Items.Select(item => item.Title));
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("asc", null, false)]
    [InlineData("desc", null, false)]
    [InlineData(null, "asc", false)]
    [InlineData(null, "desc", false)]
    [InlineData("asc", "desc", false)]
    [InlineData("desc", "asc", false)]
    [InlineData("asc", "asc", true)]
    [InlineData("desc", "desc", true)]
    public async Task GetNewsListAsync_OrdersEqualDateKeysByIdAscendingBeforePagination(
        string? createdAt,
        string? publishAt,
        bool withoutPublishDate)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var date = new DateOnly(2026, 10, 5);
        var first = CreateArticle(creator, "First", date, withoutPublishDate ? null : date);
        var second = CreateArticle(creator, "Second", date, withoutPublishDate ? null : date);
        first.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        second.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(second, first);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new NewsRequest.GetNewsListRequest
        {
            CreatedAt = createdAt,
            PublishAt = publishAt,
            PageSize = 1
        };
        var firstPage = await service.GetNewsListAsync(request);
        request.Page = 2;
        var secondPage = await service.GetNewsListAsync(request);

        Assert.Equal(2, firstPage.TotalItems);
        Assert.Equal(2, secondPage.TotalItems);
        Assert.Equal(first.Id, Assert.Single(firstPage.Items).Id);
        Assert.Equal(second.Id, Assert.Single(secondPage.Items).Id);
    }

    [Theory]
    [InlineData(null, "asc")]
    [InlineData(null, "desc")]
    [InlineData("", "asc")]
    [InlineData(" \t ", "desc")]
    public async Task GetNewsListAsync_UsesIdInsteadOfCreatedDateWhenOnlySortingPublishedDate(
        string? createdAt,
        string publishAt)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var publishDate = new DateOnly(2026, 10, 7);
        var createdFirst = CreateArticle(creator, "Created first", new DateOnly(2026, 10, 1), publishDate);
        var createdSecond = CreateArticle(creator, "Created second", new DateOnly(2026, 10, 3), publishDate);
        var createdLast = CreateArticle(creator, "Created last", new DateOnly(2026, 10, 5), publishDate);
        createdFirst.Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
        createdSecond.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        createdLast.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(createdLast, createdFirst, createdSecond);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var request = new NewsRequest.GetNewsListRequest
        {
            CreatedAt = createdAt,
            PublishAt = publishAt,
            PageSize = 2
        };
        var firstPage = await service.GetNewsListAsync(request);
        request.Page = 2;
        var secondPage = await service.GetNewsListAsync(request);

        Assert.Equal(3, firstPage.TotalItems);
        Assert.Equal(3, secondPage.TotalItems);
        Assert.Equal(new[] { createdSecond.Id, createdLast.Id }, firstPage.Items.Select(item => item.Id));
        Assert.Equal(createdFirst.Id, Assert.Single(secondPage.Items).Id);
    }

    [Fact]
    public async Task GetNewsListAsync_SerializesDatesAsOffsetTimestampsAndPreservesNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var published = CreateArticle(
            creator, "Published", new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 6));
        var draft = CreateArticle(creator, "Draft", new DateOnly(2026, 10, 5), null);
        published.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        draft.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(draft, published);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var response = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest());
        var envelope = VNZ.Service.Models.ResponseBuilder.SuccessResponse(response, "Success");
        var jsonOptions = new Microsoft.AspNetCore.Mvc.JsonOptions().JsonSerializerOptions;
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(envelope, jsonOptions));
        var items = json.RootElement.GetProperty("data").GetProperty("items");

        Assert.Equal("2026-10-05T01:00:00+00:00", items[0].GetProperty("createdAt").GetString());
        Assert.Equal("2026-10-06T02:00:00+00:00", items[0].GetProperty("publishAt").GetString());
        Assert.Equal(JsonValueKind.Null, items[1].GetProperty("publishAt").ValueKind);
    }

    private static NewsService CreateService(AppDbContext dbContext)
    {
        return new NewsService(
            dbContext,
            new StubMediaService(),
            new RichTextService.Service(),
            new VNZ.Service.Utils.SlugService.Service());
    }

    private static User CreateCreator()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = "News author",
            Email = $"{Guid.NewGuid()}@example.com",
            PasswordHash = "hash",
            EmploymentStatus = EmploymentStatus.Working,
            CreateAt = DateTimeOffset.UtcNow
        };
    }

    private static NewsArticle CreateArticle(
        User creator,
        string title,
        DateOnly createdDate,
        DateOnly? publishDate)
    {
        return new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = title,
            Status = publishDate.HasValue ? NewsStatus.Published : NewsStatus.Draft,
            Published = publishDate.HasValue,
            CreatedBy = creator.Id,
            Creator = creator,
            CreatedAt = ToVietnamUtc(createdDate, 8),
            PublishAt = publishDate.HasValue
                ? ToVietnamUtc(publishDate.Value, 9)
                : null
        };
    }

    private static DateTimeOffset ToVietnamUtc(DateOnly date, int hour)
    {
        return new DateTimeOffset(
            date.ToDateTime(new TimeOnly(hour, 0)),
            TimeSpan.FromHours(7)).ToUniversalTime();
    }

    private sealed class StubMediaService : MediaService.IService
    {
        public Task<MediaService.Response.UploadImageResponse> UploadImageAsync(
            MediaService.Request.UploadImageRequest request)
        {
            throw new NotSupportedException();
        }

        public Task<MediaService.Response.UploadAudioResponse> UploadAudioAsync(
            MediaService.Request.UploadAudioRequest request)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class NewsDateFilterTestDbContext : AppDbContext
    {
        public NewsDateFilterTestDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Content);
            modelBuilder.Entity<Product>().Ignore(product => product.Images);

            var skillsConverter = new ValueConverter<List<string>, string>(
                skills => JsonSerializer.Serialize(skills, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)!);

            modelBuilder.Entity<JobPost>()
                .Property(jobPost => jobPost.Skills)
                .HasConversion(skillsConverter);
        }
    }
}
