namespace VNZ.Service.ContactService;

public class Response
{
    public class DeleteContactResponse
    {
        public Guid Id { get; set; }
    }

    public class ContactListResponse
    {
        public List<ContactListItemResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class ContactListItemResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string ContactStatus { get; set; } = string.Empty;
    }

    public class ContactDetailResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CompanyName { get; set; }
        public string InquiryTopic { get; set; } = string.Empty;
        public string? BudgetRange { get; set; }
        public string? ExpectedStart { get; set; }
        public string? Message { get; set; }
        public string? Source { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public string ContactStatus { get; set; } = string.Empty;
        public bool CanSendEmail { get; set; }
    }

    public class SendContactReplyResponse
    {
        public Guid Id { get; set; }
        public string ContactStatus { get; set; } = string.Empty;
        public Guid ContactedBy { get; set; }
    }

    public class CreateContactInquiryResponse
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
