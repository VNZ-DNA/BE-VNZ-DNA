namespace VNZ.Service.JobApplicationService;

public static class Response
{
    public class JobApplicationListResponse
    {
        public required List<JobApplicationListItemResponse> Items { get; init; }
        public int Total { get; init; }
    }

    public class JobApplicationListItemResponse
    {
        public Guid Id { get; init; }
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public Guid JobPostId { get; init; }
        public required string JobPostTitle { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? InterviewAt { get; init; }
    }

    public class JobApplicationDetailResponse
    {
        public Guid Id { get; init; }
        public Guid JobPostId { get; init; }
        public required string JobPostTitle { get; init; }
        public required string FullName { get; init; }
        public required string Email { get; init; }
        public string? Phone { get; init; }
        public string? University { get; init; }
        public string? Major { get; init; }
        public string? CvUrl { get; init; }
        public string? PortfolioUrl { get; init; }
        public string? CoverLetter { get; init; }
        public string? JobPostSnapshot { get; init; }
        public string Status { get; init; } = string.Empty;
        public DateTimeOffset? ReviewAt { get; init; }
        public Guid? ReviewedBy { get; init; }
        public DateTimeOffset? InterviewAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
