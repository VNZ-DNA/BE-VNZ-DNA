using System.Data;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobApplicationService;

public sealed class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(
        Request.GetJobApplicationListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > 100)
        {
            throw new ArgumentException("Thông tin phân trang không hợp lệ.");
        }

        var search = request.Search?.Trim();
        if (search is { Length: > 300 })
        {
            throw new ArgumentException("Từ khóa tìm kiếm không được vượt quá 300 ký tự.");
        }

        JobApplicationStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<JobApplicationStatus>(request.Status.Trim(), false, out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                throw new ArgumentException("Trạng thái lọc không hợp lệ.");
            }

            statusFilter = parsedStatus;
        }

        var query = _dbContext.JobApplications.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(application =>
                application.FullName.ToLower().Contains(searchLower) ||
                application.Email.ToLower().Contains(searchLower) ||
                application.JobPost.Title.ToLower().Contains(searchLower));
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(application => application.Status == statusFilter.Value);
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(application => application.CreatedAt)
            .ThenByDescending(application => application.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(application => new
            {
                application.Id,
                application.FullName,
                application.Email,
                application.JobPostId,
                JobPostTitle = application.JobPost.Title,
                application.Status,
                application.CvUrl,
                CreatedAt = application.CreatedAt,
                InterviewAt = application.InterViewAt
            })
            .ToListAsync();

        var items = rows.Select(application => new Response.JobApplicationListItemResponse
        {
            Id = application.Id,
            FullName = application.FullName,
            Email = application.Email,
            JobPostId = application.JobPostId,
            JobPostTitle = application.JobPostTitle,
            Status = GetStatusLabel(application.Status),
            CvUrl = application.CvUrl,
            CreatedAt = application.CreatedAt,
            InterviewAt = application.InterviewAt,
            CanSelectForInterviewEmail = application.Status == JobApplicationStatus.Accepted
        }).ToList();

        return new Response.JobApplicationListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
        };
    }

    public async Task<Response.ReviewJobApplicationResponse> ReviewAsync(
        Guid id,
        Request.ReviewJobApplicationRequest request,
        Guid adminUserId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decision = request.Decision?.Trim();
        JobApplicationStatus nextStatus;

        if (string.Equals(decision, nameof(JobApplicationStatus.Accepted), StringComparison.OrdinalIgnoreCase))
        {
            nextStatus = JobApplicationStatus.Accepted;
        }
        else if (string.Equals(decision, nameof(JobApplicationStatus.Rejected), StringComparison.OrdinalIgnoreCase))
        {
            nextStatus = JobApplicationStatus.Rejected;
        }
        else
        {
            throw new ArgumentException(
                "Decision chỉ nhận Accepted hoặc Rejected.",
                nameof(request.Decision));
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var application = await _dbContext.JobApplications
            .Include(item => item.Reviewer)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ứng tuyển.");
        }

        if (application.Status != JobApplicationStatus.Pending)
        {
            throw new ConflictException(
                "Hồ sơ ứng tuyển không còn ở trạng thái Chờ duyệt.");
        }

        var now = DateTimeOffset.UtcNow;
        application.Status = nextStatus;
        application.ReviewedBy = adminUserId;
        application.ReviewAt = now;
        application.UpdateAt = now;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var reviewer = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == adminUserId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync();

        return new Response.ReviewJobApplicationResponse
        {
            Id = application.Id,
            Status = GetStatusLabel(application.Status),
            ReviewedByName = reviewer,
            ReviewAt = application.ReviewAt,
            CvUrl = application.CvUrl
        };
    }

    private static string GetStatusLabel(JobApplicationStatus status)
    {
        if (status == JobApplicationStatus.Pending)
        {
            return "Chờ duyệt";
        }

        if (status == JobApplicationStatus.Accepted)
        {
            return "Đã duyệt";
        }

        if (status == JobApplicationStatus.Rejected)
        {
            return "Không duyệt";
        }

        return "Đã gửi email phỏng vấn";
    }
}
