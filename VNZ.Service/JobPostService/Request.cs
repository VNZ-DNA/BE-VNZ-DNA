using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.JobPostService;

public class Request
{
    public class CreateJobPostRequest
    {
        public string? Title { get; set; }
        public Guid? DepartmentId { get; set; }
        public EmploymentType? EmploymentType { get; set; }
        public JobLevel? JobLevel { get; set; }
        public int? NumberOfPositions { get; set; }
        public List<string>? Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public DateOnly? ExpiredDate { get; set; }
        public JobPostAction? Action { get; set; }
        public JobPostTranslationsRequest? Translations { get; set; }
    }

    public class GetJobPostListRequest
    {
        public string? Search { get; set; }
        public List<string>? Status { get; set; }
        public List<string>? DepartmentId { get; set; }
        public List<string>? JobLevel { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class UpdateJobPostRequest
    {
        public Guid? DepartmentId { get; set; }
        public string? Title { get; set; }
        public EmploymentType? EmploymentType { get; set; }
        public JobLevel? JobLevel { get; set; }
        public int? NumberOfPositions { get; set; }
        public List<string>? Skills { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public DateOnly? ExpiredDate { get; set; }
        public JobPostAction? Action { get; set; }
        public DateTimeOffset? ExpectedUpdatedAt { get; set; }
        public JobPostTranslationsRequest? Translations { get; set; }
    }

    public class JobPostTranslationsRequest
    {
        public JobPostEnglishTranslationRequest? En { get; set; }
    }

    public class JobPostEnglishTranslationRequest
    {
        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
    }
}
