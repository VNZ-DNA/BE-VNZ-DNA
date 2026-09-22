namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class NewsCategory : BaseEntity
{
    public string Name { get; set; } = null!;
    public DateTime CreateAt { get; set; }

    public ICollection<NewsArticleCategory> NewsArticleCategories { get; set; } = new List<NewsArticleCategory>();
}
