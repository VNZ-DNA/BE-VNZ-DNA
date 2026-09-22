namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class Partner : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public string Name { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Description { get; set; }
    public bool IsPublished { get; set; }
    public int? DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public User? Creator { get; set; }
}
