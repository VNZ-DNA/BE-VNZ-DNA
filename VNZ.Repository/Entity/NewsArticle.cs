namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class NewsArticle : BaseEntity
{
    public string Title { get; set; } = null!;
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public NewsStatus Status { get; set; }
    public bool Published { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishAt { get; set; }

    public User? Creator { get; set; }
    public ICollection<NewsArticleCategory> NewsArticleCategories { get; set; } = new List<NewsArticleCategory>();
}
