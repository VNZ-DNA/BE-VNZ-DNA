using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
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

    public async Task<Response.ReviewJobApplicationResponse> ReviewAsync(
        Guid id,
        Request.ReviewJobApplicationRequest request,
        Guid adminUserId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var decision = request.Decision?.Trim().ToLowerInvariant();
        JobApplicationStatus nextStatus;

        if (decision == "accepted")
        {
            nextStatus = JobApplicationStatus.Accepted;
        }
        else if (decision == "rejected")
        {
            nextStatus = JobApplicationStatus.Rejected;
        }
        else
        {
            throw new ArgumentException("Decision chỉ nhận Accepted hoặc Rejected.", nameof(request.Decision));
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var application = await _dbContext.JobApplications.SingleOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");
        }

        if (application.Status != JobApplicationStatus.Pending)
        {
            throw new ConflictException("Hồ sơ ứng viên không còn ở trạng thái Chờ duyệt.");
        }

        var now = DateTimeOffset.UtcNow;
        application.Status = nextStatus;
        application.ReviewedBy = adminUserId;
        application.ReviewAt = now;
        application.UpdateAt = now;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        var reviewerName = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == adminUserId)
            .Select(user => user.FullName)
            .SingleOrDefaultAsync();

        return new Response.ReviewJobApplicationResponse
        {
            Id = application.Id,
            Status = GetStatusLabel(application.Status),
            ReviewedByName = reviewerName,
            ReviewAt = application.ReviewAt,
            CvUrl = application.CvUrl
        };
    }

    public async Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(Request.GetJobApplicationListRequest request)
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
            if (!Enum.TryParse<JobApplicationStatus>(request.Status.Trim(), ignoreCase: false, out var parsedStatus) ||
                !Enum.IsDefined(parsedStatus))
            {
                throw new ArgumentException("Trạng thái lọc không hợp lệ.");
            }

            statusFilter = parsedStatus;
        }

        var query = _dbContext.JobApplications
            .AsNoTracking()
            .AsQueryable();

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

        var applicationRows = await query
            .OrderByDescending(application => application.CreatedAt)
            .ThenByDescending(application => application.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(application => new
            {
                Id = application.Id,
                FullName = application.FullName,
                Email = application.Email,
                JobPostId = application.JobPostId,
                JobPostTitle = application.JobPost.Title,
                Status = application.Status,
                CvUrl = application.CvUrl,
                CreatedAt = application.CreatedAt,
                InterviewAt = application.InterViewAt,
                CanSelectForInterviewEmail = application.Status == JobApplicationStatus.Accepted
            })
            .ToListAsync();

        var applications = applicationRows.Select(application => new Response.JobApplicationListItemResponse
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
            CanSelectForInterviewEmail = application.CanSelectForInterviewEmail
        }).ToList();

        return new Response.JobApplicationListResponse
        {
            Items = applications,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
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

    public async Task<Response.JobApplicationDetailResponse> GetJobApplicationByIdAsync(Guid id)
    {
        var application = await _dbContext.JobApplications
            .AsNoTracking()
            .Include(item => item.JobPost)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            throw new NotFoundException("Không tìm thấy hồ sơ ứng viên.");
        }

        return new Response.JobApplicationDetailResponse
        {
            Id = application.Id,
            JobPostId = application.JobPostId,
            JobPostTitle = application.JobPost.Title,
            FullName = application.FullName,
            Email = application.Email,
            Phone = application.Phone,
            University = application.University,
            Major = application.Major,
            CvUrl = application.CvUrl,
            PortfolioUrl = application.PortfolioUrl,
            CoverLetter = application.CoverLetter,
            JobPostSnapshot = application.JobPostSnapshot,
            Status = application.Status.ToString(),
            ReviewAt = application.ReviewAt,
            ReviewedBy = application.ReviewedBy,
            InterviewAt = application.InterViewAt,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdateAt
        };
    }
}
