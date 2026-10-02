using System.Text.Json;
using System.Text.Json.Serialization;

namespace VNZ.Service.JobApplicationService;

public static class Request
{
    public class CreateJobApplicationRequest
    {
        public Guid? JobPostId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? GraduationYear { get; set; }
        public string? University { get; set; }
        public string? Major { get; set; }
        public string? CvUrl { get; set; }
        public string? PortfolioUrl { get; set; }
        public string? CoverLetter { get; set; }
        public string? Availability { get; set; }
        public string? AvailableStartDate { get; set; }
        public string? ReferralSource { get; set; }
        public bool ConsentToDataProcessing { get; set; }
    }

    public class GetJobApplicationListRequest
    {
        public string? Search { get; set; }
        public List<string>? Status { get; set; }
        public List<Guid>? JobPostId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class ReviewJobApplicationRequest
    {
        public string? Decision { get; set; }
    }

    public class SendInterviewInvitationsRequest
    {
        public List<Guid>? ApplicationIds { get; set; }
        public string? InterviewDate { get; set; }
        public string? InterviewTime { get; set; }
        public int? DurationMinutes { get; set; }
        public string? InterviewMode { get; set; }
        public string? Location { get; set; }
        public string? LocationUrl { get; set; }
        public string? InterviewInformationHtml { get; set; }
        public string? AgendaHtml { get; set; }
        public string? PreparationHtml { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
    }
}
