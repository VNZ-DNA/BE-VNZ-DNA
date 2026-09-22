namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;

public class Department : BaseEntity
{
    public string Name { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<JobPost> JobPosts { get; set; } = new List<JobPost>();
}
