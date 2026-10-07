namespace VNZ.Service.JobPostService;

public class Response
{
    public class DeleteJobPostResponse
    {
        public Guid Id { get; set; }
    }

    public class CreateJobPostResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public required string Status { get; set; }
        public DateOnly? ExpiredDate { get; set; }
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

    public class PublicJobPostListItemResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public required string Title { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? JobLevel { get; set; }
        public int NumberOfPositions { get; set; }
        public required List<string> Skills { get; set; }
        public string? ShortDescription { get; set; }
        public DateOnly ExpiredDate { get; set; }
    }

    public class JobPostListItemResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
        public DateOnly? ExpiredDate { get; set; }
        public int NumberOfPositions { get; set; }
        public required string Status { get; set; }
        public int PendingApplicationCount { get; set; }
        public bool CanDelete { get; set; }
        public string? DeleteBlockedReason { get; set; }
    }

    public class PublicJobPostDetailResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public required string Title { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? JobLevel { get; set; }
        public int NumberOfPositions { get; set; }
        public required List<string> Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public DateOnly ExpiredDate { get; set; }
    }

    public class JobPostDetailResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? CreatedByName { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public required string Status { get; set; }
        public DateOnly? ExpiredDate { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? JobLevel { get; set; }
        public int NumberOfPositions { get; set; }
        public required List<string> Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public bool CanEdit { get; set; }
        public JobPostTranslationsResponse? Translations { get; set; }
        public bool CanDelete { get; set; }
        public string? DeleteBlockedReason { get; set; }
    }

    public class UpdateJobPostResponse
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? CreatedByName { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public required string Status { get; set; }
        public DateOnly? ExpiredDate { get; set; }
        public string? Department { get; set; }
        public string? EmploymentType { get; set; }
        public string? JobLevel { get; set; }
        public int NumberOfPositions { get; set; }
        public required List<string> Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public JobPostTranslationsResponse? Translations { get; set; }
    }

    public class JobPostTranslationsResponse
    {
        public JobPostEnglishTranslationResponse? En { get; set; }
    }

    public class JobPostEnglishTranslationResponse
    {
        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
    }
}
