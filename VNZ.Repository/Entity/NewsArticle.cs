namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;

public class NewsArticle : BaseEntity
{
    public string? Title { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public int ReadingTimeMinutes { get; set; } = 1;
    public NewsStatus Status { get; set; }
    public bool Published { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? PublishAt { get; set; }
    public NewsTranslations? Translations { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<NewsArticleCategory> NewsArticleCategories { get; set; } = new List<NewsArticleCategory>();
}
