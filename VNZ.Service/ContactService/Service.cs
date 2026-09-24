using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.ContactService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

        // 3. Map the optional UI status label to the domain enum.
        ContactStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();

            if (status == "Chưa liên hệ")
            {
                statusFilter = ContactStatus.NotContacted;
            }
            else if (status == "Đã liên hệ")
            {
                statusFilter = ContactStatus.Contacted;
            }
            else
            {
                throw new ContactException(
                    "CONTACT_QUERY_INVALID",
                    "Thông tin truy vấn không hợp lệ.",
                    "status");
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

        if (statusFilter.HasValue)
        {
            query = query.Where(contact =>
                contact.ContactStatus == statusFilter.Value);
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
