namespace VNZ.Service.JobApplicationService;

public static class Response
{
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
