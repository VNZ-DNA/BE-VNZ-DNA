namespace VNZ.Service.JobPostService;

public class Response
{
    public class CreateJobPostResponse
    {
        public Guid Id { get; set; }
        public required string Status { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsPubliclyVisible { get; set; }
    }

    public class PagedJobPostListResponse
    {
        public required List<JobPostListItemResponse> Items { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class JobPostListItemResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? ShortDescription { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public int NumberOfPositions { get; set; }
        public required string Status { get; set; }
        public int PendingApplicationCount { get; set; }
    }

    public class JobPostDetailResponse
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? CreatedByName { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public required string Status { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? JobLevel { get; set; }
        public int NumberOfPositions { get; set; }
        public required List<string> Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public bool CanEdit { get; set; }
    }
}
