namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class JobPost : BaseEntity
{
    public Guid? DepartmentId { get; set; }
    public Guid? CreatedBy { get; set; }
    public JobPostStatus Status { get; set; }
    public DateTime? ExpiredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string Title { get; set; } = null!;
    public EmploymentType? EmploymentType { get; set; }
    public List<string> Skills { get; set; } = new();
    public JobLevel? JobLevel { get; set; }
    public int NumberOfPositions { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }

    public Department? Department { get; set; }
    public User? Creator { get; set; }
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
