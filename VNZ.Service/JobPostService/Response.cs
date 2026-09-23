namespace VNZ.Service.JobPostService;

public class Response
{
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
}
