namespace VNZ.Repository.Entity;

using VNZ.Repository.Abstraction;
using VNZ.Repository.Entity.Enum;

public class ContactInquiry : BaseEntity
{
    public ContactInquiryTopic InquiryTopic { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string? CompanyName { get; set; }
    public ContactBudgetRange? BudgetRange { get; set; }
    public ContactExpectedStart? ExpectedStart { get; set; }
    public string? Message { get; set; }
    public ContactSource? Source { get; set; }
    public bool IsRead { get; set; }
    public ContactStatus ContactStatus { get; set; } = ContactStatus.NotContacted;
    public Guid? ContactedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User? ContactedByUser { get; set; }
}
