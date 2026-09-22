namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class Role : BaseEntity
{
    public string Type { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
