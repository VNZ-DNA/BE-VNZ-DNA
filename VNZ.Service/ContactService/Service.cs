using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;
using MailService = VNZ.Service.MailService;

namespace VNZ.Service.ContactService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;
    private readonly MailService.IService _mailService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MailService.IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly VNZ.Service.Utils.RichTextService.IService _richTextService;

    public Service(
        AppDbContext dbContext,
        MailService.IService mailService,
        IHttpContextAccessor httpContextAccessor)
        : this(
            dbContext,
            mailService,
            httpContextAccessor,
            new MailService.EmailTemplateRenderer(),
            new VNZ.Service.Utils.RichTextService.Service())
    {
    }

    public Service(
        AppDbContext dbContext,
        MailService.IService mailService,
        IHttpContextAccessor httpContextAccessor,
        MailService.IEmailTemplateRenderer emailTemplateRenderer,
        VNZ.Service.Utils.RichTextService.IService richTextService)
    {
        _dbContext = dbContext;
        _mailService = mailService;
        _httpContextAccessor = httpContextAccessor;
        _emailTemplateRenderer = emailTemplateRenderer;
        _richTextService = richTextService;
    }

    public async Task<Response.DeleteContactResponse> DeleteContactAsync(Guid id)
    {
        var affectedRows = await _dbContext.ContactInquiries
            .Where(contact => contact.Id == id && !contact.IsDelete)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(contact => contact.IsDelete, true));

        if (affectedRows != 1)
        {
            throw new ContactException("CONTACT_NOT_FOUND", "Không tìm thấy yêu cầu liên hệ.", "id");
        }

        return new Response.DeleteContactResponse { Id = id };
    }

    public async Task<Response.ContactListResponse> GetContactListAsync(
        Request.GetContactListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate pagination before querying the database.
        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
        {
            var fields = new List<string>();

            if (request.Page < 1)
            {
                fields.Add("page");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                fields.Add("pageSize");
            }

            throw new ContactException(
                "CONTACT_QUERY_INVALID",
                "Thông tin truy vấn không hợp lệ.",
                fields.ToArray());
        }

        // 2. Normalize and validate the optional search keyword.
        var search = request.Search?.Trim();
        if (search is { Length: > 300 })
        {
            throw new ContactException(
                "CONTACT_QUERY_INVALID",
                "Thông tin truy vấn không hợp lệ.",
                "search");
        }

        // 3. Map các filter multi-select từ UI sang giá trị domain.
        var statusFilters = new List<ContactStatus>();
        var statuses = request.Status?
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => status.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        foreach (var status in statuses)
        {
            if (status == "Chưa liên hệ")
            {
                if (!statusFilters.Contains(ContactStatus.NotContacted))
                {
                    statusFilters.Add(ContactStatus.NotContacted);
                }
            }
            else if (status == "Đã liên hệ")
            {
                if (!statusFilters.Contains(ContactStatus.Contacted))
                {
                    statusFilters.Add(ContactStatus.Contacted);
                }
            }
            else
            {
                throw new ContactException(
                    "CONTACT_QUERY_INVALID",
                    "Thông tin truy vấn không hợp lệ.",
                    "status");
            }
        }

        var isReadFilters = new List<bool>();
        var isReadValues = request.IsRead?
            .Where(isRead => !string.IsNullOrWhiteSpace(isRead))
            .Select(isRead => isRead.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        foreach (var isReadValue in isReadValues)
        {
            var isValidIsRead = bool.TryParse(isReadValue, out var isRead);

            if (!isValidIsRead)
            {
                throw new ContactException(
                    "CONTACT_QUERY_INVALID",
                    "Thông tin truy vấn không hợp lệ.",
                    "isRead");
            }

            if (!isReadFilters.Contains(isRead))
            {
                isReadFilters.Add(isRead);
            }
        }

        // 4. Apply filters before counting and paging the live ContactInquiry data.
        var query = _dbContext.ContactInquiries
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(contact =>
                contact.FullName.ToLower().Contains(searchLower) ||
                (contact.CompanyName != null &&
                    contact.CompanyName.ToLower().Contains(searchLower)) ||
                contact.Email.ToLower().Contains(searchLower));
        }

        if (statusFilters.Count > 0)
        {
            query = query.Where(contact =>
                statusFilters.Contains(contact.ContactStatus));
        }

        if (isReadFilters.Count > 0)
        {
            query = query.Where(contact =>
                isReadFilters.Contains(contact.IsRead));
        }

        var totalItems = await query.CountAsync();

        var contacts = await query
            .OrderByDescending(contact => contact.CreatedAt)
            .ThenBy(contact => contact.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(contact => new
            {
                contact.Id,
                contact.FullName,
                contact.CompanyName,
                contact.Email,
                contact.CreatedAt,
                contact.IsRead,
                contact.ContactStatus
            })
            .ToListAsync();

        var items = contacts
            .Select(contact => new Response.ContactListItemResponse
            {
                Id = contact.Id,
                FullName = contact.FullName,
                CompanyName = contact.CompanyName,
                Email = contact.Email,
                CreatedAt = contact.CreatedAt,
                IsRead = contact.IsRead,
                ContactStatus = GetDisplayName(contact.ContactStatus)
            })
            .ToList();

        return new Response.ContactListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = totalItems,
            TotalPages = totalItems == 0
                ? 0
                : (int)Math.Ceiling(totalItems / (double)request.PageSize)
        };
    }

    public async Task<Response.ContactDetailResponse> GetContactDetailAsync(Guid id)
    {
        // 1. Read the entity as tracked because opening an unread contact changes IsRead.
        var contact = await _dbContext.ContactInquiries
            .SingleOrDefaultAsync(contact => contact.Id == id);

        if (contact is null)
        {
            throw new ContactException(
                "CONTACT_NOT_FOUND",
                "Không tìm thấy yêu cầu liên hệ.",
                "id");
        }

        // 2. Only opening an unread detail marks it as read.
        if (!contact.IsRead)
        {
            contact.IsRead = true;
            await _dbContext.SaveChangesAsync();
        }

        // 3. Return the persisted state without changing ContactStatus or ContactedBy.
        return new Response.ContactDetailResponse
        {
            Id = contact.Id,
            FullName = contact.FullName,
            Email = contact.Email,
            Phone = contact.Phone,
            CompanyName = contact.CompanyName,
            InquiryTopic = GetDisplayName(contact.InquiryTopic),
            BudgetRange = contact.BudgetRange.HasValue
                ? GetDisplayName(contact.BudgetRange.Value)
                : null,
            ExpectedStart = contact.ExpectedStart.HasValue
                ? GetDisplayName(contact.ExpectedStart.Value)
                : null,
            Message = contact.Message,
            Source = contact.Source.HasValue
                ? GetDisplayName(contact.Source.Value)
                : null,
            CreatedAt = contact.CreatedAt,
            IsRead = contact.IsRead,
            ContactStatus = GetDisplayName(contact.ContactStatus),
            CanSendEmail = contact.ContactStatus == ContactStatus.NotContacted
        };
    }

    public Task<VNZ.Service.MailService.Response.ContactTemplateSchemaResponse> GetReplyTemplateAsync()
    {
        return Task.FromResult(VNZ.Service.MailService.EmailTemplateSchemaProvider.GetContactSchema());
    }

    public async Task<Response.CreateContactInquiryResponse> CreateContactInquiryAsync(
        Request.CreateContactInquiryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Normalize input values before validation and persistence.
        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var phone = request.Phone?.Trim();
        var companyName = request.CompanyName?.Trim();
        var message = request.Message?.Trim();

        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = null;
        }

        if (string.IsNullOrWhiteSpace(companyName))
        {
            companyName = null;
        }

        var fields = new List<string>();

        // 2. Validate required values, enums and the fields persisted as text.
        if (!request.InquiryTopic.HasValue
            || !Enum.IsDefined(request.InquiryTopic.Value))
        {
            fields.Add("inquiryTopic");
        }

        if (string.IsNullOrWhiteSpace(fullName)
            || fullName.Length > 200
            || fullName.Contains('\0'))
        {
            fields.Add("fullName");
        }

        if (string.IsNullOrWhiteSpace(email)
            || email.Length > 320
            || email.Contains('\0')
            || !new EmailAddressAttribute().IsValid(email))
        {
            fields.Add("email");
        }

        if (phone is not null)
        {
            if (phone.StartsWith("+84", StringComparison.Ordinal))
            {
                phone = $"0{phone[3..]}";
            }
            else if (phone.StartsWith("84", StringComparison.Ordinal))
            {
                phone = $"0{phone[2..]}";
            }

            phone = phone
                .Replace(" ", string.Empty)
                .Replace(".", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty);

            if (phone.Contains('\0')
                || !Regex.IsMatch(phone, @"\A(?:0[35789][0-9]{8}|02[0-9]{9})\z"))
            {
                fields.Add("phone");
            }
        }

        if (companyName is not null
            && (companyName.Length > 200 || companyName.Contains('\0')))
        {
            fields.Add("companyName");
        }

        if (request.BudgetRange.HasValue
            && !Enum.IsDefined(request.BudgetRange.Value))
        {
            fields.Add("budgetRange");
        }

        if (request.ExpectedStart.HasValue
            && !Enum.IsDefined(request.ExpectedStart.Value))
        {
            fields.Add("expectedStart");
        }

        if (string.IsNullOrWhiteSpace(message) || message.Contains('\0'))
        {
            fields.Add("message");
        }

        if (request.Source.HasValue && !Enum.IsDefined(request.Source.Value))
        {
            fields.Add("source");
        }

        if (request.ConsentToDataProcessing != true)
        {
            fields.Add("consentToDataProcessing");
        }

        if (fields.Count > 0)
        {
            throw new ContactException(
                "CONTACT_CREATE_VALIDATION_FAILED",
                "Thông tin liên hệ không hợp lệ.",
                fields.Distinct().ToArray());
        }

        // 3. Set all server-owned values and save one new inquiry.
        var contact = new VNZ.Repository.Entity.ContactInquiry
        {
            Id = Guid.NewGuid(),
            InquiryTopic = request.InquiryTopic!.Value,
            FullName = fullName!,
            Email = email!,
            Phone = phone,
            CompanyName = companyName,
            BudgetRange = request.BudgetRange,
            ExpectedStart = request.ExpectedStart,
            Message = message!,
            Source = request.Source,
            ConsentToDataProcessing = true,
            IsRead = false,
            ContactStatus = ContactStatus.NotContacted,
            ContactedBy = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.ContactInquiries.Add(contact);
        await _dbContext.SaveChangesAsync();

        return new Response.CreateContactInquiryResponse
        {
            Id = contact.Id,
            CreatedAt = contact.CreatedAt
        };
    }

    public async Task<Response.SendContactReplyResponse> SendReplyAsync(
        Guid id,
        Request.SendContactReplyRequest request)
    {
        var contactedBy = GetAdminId();

        ValidateAdditionalFields(request?.AdditionalFields);

        var normalized = ValidateReplyRequest(request);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // PostgreSQL khóa dòng Contact này đến khi transaction commit hoặc rollback.
        // Request phản hồi đồng thời phải chờ, sau đó sẽ thấy ContactStatus đã được cập nhật.
        var contact = await _dbContext.ContactInquiries
            .FromSqlInterpolated($"""SELECT * FROM "Contact_Inquiry" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync();

        if (contact is null)
        {
            throw new ContactException(
                "CONTACT_NOT_FOUND",
                "Không tìm thấy yêu cầu liên hệ.",
                "id");
        }

        if (contact.ContactStatus == ContactStatus.Contacted)
        {
            throw new ContactException(
                "CONTACT_ALREADY_CONTACTED",
                "Yêu cầu liên hệ này đã được phản hồi.");
        }

        var renderedEmail = _emailTemplateRenderer.RenderContact(new MailService.ContactEmailTemplateData
        {
            RecipientName = contact.FullName,
            Subject = normalized.Subject,
            BodyHtml = normalized.BodyHtml,
            ProposalHtml = normalized.ProposalHtml,
            NextStepsHtml = normalized.NextStepsHtml
        });

        await _mailService.SendAsync(new MailService.MailContent
        {
            To = contact.Email,
            ToName = contact.FullName,
            Subject = renderedEmail.Subject,
            Body = renderedEmail.HtmlBody,
            IdempotencyKey = $"contact-reply-{contact.Id}",
            IsHtmlBody = true,
            Tag = "contact-reply"
        });

        contact.ContactStatus = ContactStatus.Contacted;
        contact.ContactedBy = contactedBy;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new Response.SendContactReplyResponse
        {
            Id = contact.Id,
            ContactStatus = GetDisplayName(contact.ContactStatus),
            ContactedBy = contactedBy
        };
    }

    private NormalizedReplyContent ValidateReplyRequest(
        Request.SendContactReplyRequest? request)
    {
        var fields = new List<string>();
        var subject = request?.Subject?.Trim();
        var body = request?.Body;
        var proposalHtml = request?.ProposalHtml;
        var nextStepsHtml = request?.NextStepsHtml;

        if (string.IsNullOrWhiteSpace(subject)
            || subject.Length > 200
            || subject.Contains('<')
            || subject.Contains('>')
            || subject.Contains('\r')
            || subject.Contains('\n'))
        {
            fields.Add("subject");
        }

        var sanitizedBody = NormalizeEmailRichText(body, "body", required: true, fields);
        var sanitizedProposal = NormalizeEmailRichText(proposalHtml, "proposalHtml", required: false, fields);
        var sanitizedNextSteps = NormalizeEmailRichText(nextStepsHtml, "nextStepsHtml", required: false, fields);

        if (fields.Count > 0)
        {
            throw new ContactException(
                "CONTACT_REPLY_VALIDATION_ERROR",
                "Tiêu đề và nội dung email không hợp lệ.",
                fields.ToArray());
        }

        return new NormalizedReplyContent
        {
            Subject = subject!,
            BodyHtml = sanitizedBody!,
            ProposalHtml = sanitizedProposal,
            NextStepsHtml = sanitizedNextSteps
        };
    }

    private static void ValidateAdditionalFields(
        Dictionary<string, System.Text.Json.JsonElement>? additionalFields)
    {
        if (additionalFields is { Count: > 0 })
        {
            throw new ContactException(
                "CONTACT_REPLY_VALIDATION_ERROR",
                "Payload chứa field không thuộc contract email phản hồi.",
                additionalFields.Keys.ToArray());
        }
    }

    private string? NormalizeEmailRichText(
        string? value,
        string field,
        bool required,
        ICollection<string> fields)
    {
        if (value is { Length: > 20_000 })
        {
            fields.Add(field);
            return null;
        }

        var sanitized = _richTextService.SanitizeEmail(value);
        if (required && string.IsNullOrWhiteSpace(sanitized))
        {
            fields.Add(field);
        }

        return sanitized;
    }

    private sealed class NormalizedReplyContent
    {
        public string Subject { get; init; } = string.Empty;
        public string BodyHtml { get; init; } = string.Empty;
        public string? ProposalHtml { get; init; }
        public string? NextStepsHtml { get; init; }
    }

    private Guid GetAdminId()
    {
        var adminId = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(adminId, out var contactedBy))
        {
            throw new AuthException(
                "AUTH_UNAUTHENTICATED",
                "Yêu cầu đăng nhập để tiếp tục.");
        }

        return contactedBy;
    }

    private static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).Single();

        return member.GetCustomAttributes(typeof(DisplayAttribute), inherit: false)
            .OfType<DisplayAttribute>()
            .SingleOrDefault()?
            .GetName() ?? value.ToString();
    }
}
