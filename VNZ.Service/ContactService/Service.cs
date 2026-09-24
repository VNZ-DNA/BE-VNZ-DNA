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

    private static string GetDisplayName(ContactStatus status)
    {
        var member = typeof(ContactStatus).GetMember(status.ToString()).Single();

        return member.GetCustomAttributes(typeof(DisplayAttribute), inherit: false)
            .OfType<DisplayAttribute>()
            .SingleOrDefault()?
            .GetName() ?? status.ToString();
    }
}
