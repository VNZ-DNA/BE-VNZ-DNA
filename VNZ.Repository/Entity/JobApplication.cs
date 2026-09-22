namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class JobApplication : BaseEntity
{
    public Guid JobPostId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? University { get; set; }
    public string? Major { get; set; }
    public string? CvUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? CoverLetter { get; set; }
    public string? JobPostSnapshot { get; set; }
    public JobApplicationStatus Status { get; set; }
    public DateTime? ReviewAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? InterViewAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdateAt { get; set; }

    public JobPost JobPost { get; set; } = null!;
    public User? Reviewer { get; set; }
}
