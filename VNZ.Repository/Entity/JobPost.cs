namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;

public class JobPost : BaseEntity
{
    public Guid? DepartmentId { get; set; }
    public Guid? CreatedBy { get; set; }
    public JobPostStatus Status { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? Title { get; set; }
    public string Slug { get; set; } = string.Empty;
    public EmploymentType? EmploymentType { get; set; }
    public List<string> Skills { get; set; } = new();
    public JobLevel? JobLevel { get; set; }
    public int NumberOfPositions { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public JobPostTranslations? Translations { get; set; }

    public Department? Department { get; set; }
    public User? Creator { get; set; }
    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
