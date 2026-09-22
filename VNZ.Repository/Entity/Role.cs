namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class Role : BaseEntity
{
    public string Type { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
