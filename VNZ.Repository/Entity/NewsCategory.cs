namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class NewsCategory : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTimeOffset CreateAt { get; set; }

    public ICollection<NewsArticleCategory> NewsArticleCategories { get; set; } = new List<NewsArticleCategory>();
}
