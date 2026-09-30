using VNZ.Repository.Entity.Json;

namespace VNZ.Service.JobApplicationService;

public static class Response
{
    public class CreateJobApplicationResponse
    {
        public Guid Id { get; set; }
        public Guid JobPostId { get; set; }
        public required string JobPostTitle { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public int? GraduationYear { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public required string CvUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public required string CoverLetter { get; set; }
        public string? Availability { get; set; }
        public string? AvailableStartDate { get; set; }
        public string? ReferralSource { get; set; }
        public bool ConsentToDataProcessing { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset? ReviewAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTimeOffset? InterviewAt { get; set; }
        public JobPostSnapshot? JobPostSnapshot { get; set; }
    }

    public class JobApplicationListResponse
    {
        public required List<JobApplicationListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class JobApplicationListItemResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public Guid JobPostId { get; set; }
        public required string JobPostTitle { get; set; }
        public string? JobPostSnapshotTitle { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CvUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? InterviewAt { get; set; }
        public bool CanSelectForInterviewEmail { get; set; }
    }

    public class ReviewJobApplicationResponse
    {
        public Guid Id { get; set; }
        public required string Status { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewAt { get; set; }
        public string? CvUrl { get; set; }
    }

    public class SendInterviewInvitationsResponse
    {
        public DateTimeOffset InterviewAt { get; set; }
        public int TotalRequested { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public required List<InterviewInvitationResultResponse> Results { get; set; }
    }

    public class InterviewInvitationResultResponse
    {
        public Guid ApplicationId { get; set; }
        public required string Outcome { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset? InterviewAt { get; set; }
    }

    public class JobApplicationDetailResponse
    {
        public Guid Id { get; set; }
        public Guid JobPostId { get; set; }
        public required string JobPostTitle { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
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
        public bool ConsentToDataProcessing { get; set; }
        public JobPostSnapshot? JobPostSnapshot { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset? ReviewAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTimeOffset? InterviewAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
