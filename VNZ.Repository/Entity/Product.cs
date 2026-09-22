namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class Product : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public string? ProductUrl { get; set; }
    public ProductStatus Status { get; set; }
    public bool IsPublished { get; set; }
    public int? DisplayOrder { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Content { get; set; }

    public User? Creator { get; set; }
}
