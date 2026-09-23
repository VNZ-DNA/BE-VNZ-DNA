using System.Text.Json;

namespace VNZ.Service.JobApplicationService;

public class Response
{
    public class JobApplicationDetailResponse
    {
        public Guid Id { get; set; }
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public string? Phone { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public string? CvUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? CoverLetter { get; set; }
        public required string Status { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewAt { get; set; }
        public DateTimeOffset? InterViewAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public JsonElement? JobPostSnapshot { get; set; }
    }

    public class ReviewJobApplicationResponse
    {
        public Guid Id { get; set; }
        public required string Status { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTimeOffset? ReviewAt { get; set; }
        public string? CvUrl { get; set; }
    }
}
