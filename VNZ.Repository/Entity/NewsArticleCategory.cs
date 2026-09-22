namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class NewsArticleCategory : BaseEntity
{
    public Guid NewsArticleId { get; set; }
    public Guid NewsCategoryId { get; set; }

    public NewsArticle NewsArticle { get; set; } = null!;
    public NewsCategory NewsCategory { get; set; } = null!;
}
