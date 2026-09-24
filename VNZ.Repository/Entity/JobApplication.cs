namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;

public class JobApplication : BaseEntity
{
    public Guid JobPostId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? University { get; set; }
    public string? Major { get; set; }
    public int? GraduationYear { get; set; }
    public string? Availability { get; set; }
    public string? AvailableStartDate { get; set; }
    public string? ReferralSource { get; set; }
    public string? CvUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string? CoverLetter { get; set; }
    public JobPostSnapshot? JobPostSnapshot { get; set; }
    public JobApplicationStatus Status { get; set; }
    public DateTimeOffset? ReviewAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTimeOffset? InterViewAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdateAt { get; set; }

    public JobPost JobPost { get; set; } = null!;
    public User? Reviewer { get; set; }
}
