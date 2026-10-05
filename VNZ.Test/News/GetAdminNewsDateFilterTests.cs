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
    [Fact]
    public async Task GetNewsListAsync_FiltersByCreatedAndPublishedVietnamDates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new NewsDateFilterTestDbContext(options);
        var creator = CreateCreator();
        var expectedArticle = CreateArticle(
            creator,
            "Expected article",
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 6));
        var wrongPublishedDate = CreateArticle(
            creator,
            "Wrong published date",
            new DateOnly(2026, 10, 5),
            new DateOnly(2026, 10, 7));
        var wrongCreatedDate = CreateArticle(
            creator,
            "Wrong created date",
            new DateOnly(2026, 10, 4),
            new DateOnly(2026, 10, 6));

        dbContext.Users.Add(creator);
        dbContext.NewsArticles.AddRange(
            expectedArticle,
            wrongPublishedDate,
            wrongCreatedDate);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetNewsListAsync(new NewsRequest.GetNewsListRequest
        {
            CreatedAt = "2026-10-05",
            PublishAt = "2026-10-06"
        });

        Assert.Equal(1, response.TotalItems);
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
        var publishAtAscending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { PublishAt = "asc" });
        var publishAtDescending = await service.GetNewsListAsync(
            new NewsRequest.GetNewsListRequest { PublishAt = "desc" });

        Assert.Equal(
            new[] { createdFirst.Id, withoutPublishedDate.Id, createdSecond.Id },
            createdAtAscending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { createdSecond.Id, createdFirst.Id, withoutPublishedDate.Id },
            publishAtAscending.Items.Select(item => item.Id));
        Assert.Equal(
            new[] { createdFirst.Id, createdSecond.Id, withoutPublishedDate.Id },
            publishAtDescending.Items.Select(item => item.Id));
    }

    [Theory]
    [InlineData("05/10/2026", "createdAt")]
    [InlineData("2026-13-01", "publishAt")]
    public async Task GetNewsListAsync_RejectsInvalidDateFilter(string value, string field)
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

    private static NewsService CreateService(AppDbContext dbContext)
    {
        return new NewsService(
            dbContext,
            new StubMediaService(),
            new RichTextService.Service());
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
