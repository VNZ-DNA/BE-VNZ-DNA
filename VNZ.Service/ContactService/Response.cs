namespace VNZ.Service.ContactService;

public class Response
{
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
}
