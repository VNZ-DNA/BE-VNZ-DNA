using System.Text.Json;
using System.Text.Json.Serialization;
using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.ContactService;

public class Request
{
    public class GetContactListRequest
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class SendContactReplyRequest
    {
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? ProposalHtml { get; set; }
        public string? NextStepsHtml { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalFields { get; set; }
    }

    public class CreateContactInquiryRequest
    {
        public ContactInquiryTopic? InquiryTopic { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public ContactBudgetRange? BudgetRange { get; set; }
        public ContactExpectedStart? ExpectedStart { get; set; }
        public string? Message { get; set; }
        public ContactSource? Source { get; set; }
        public bool? ConsentToDataProcessing { get; set; }
    }
}
