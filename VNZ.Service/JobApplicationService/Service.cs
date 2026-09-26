using System.Globalization;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobApplicationService;

public sealed class Service : IService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly MailService.IService _mailService;
    private readonly ILogger<Service> _logger;

    public Service(
        AppDbContext dbContext,
        MailService.IService mailService,
        ILogger<Service> logger)
    {
        _dbContext = dbContext;
        _mailService = mailService;
        _logger = logger;
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
            Status = GetDisplayName(application.Status),
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
            Status = GetDisplayName(application.Status),
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

    public async Task<Response.SendInterviewInvitationsResponse> SendInterviewInvitationsAsync(
        Request.SendInterviewInvitationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var applicationIds = ValidateApplicationIds(request.ApplicationIds);
        var interviewAt = ParseInterviewAt(request.InterviewDate, request.InterviewTime);

        if (interviewAt <= DateTimeOffset.UtcNow.ToOffset(VietnamOffset))
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_TIME_INVALID",
                "Lịch phỏng vấn phải lớn hơn thời điểm hiện tại.",
                nameof(request.InterviewDate),
                nameof(request.InterviewTime));
        }

        var applications = await _dbContext.JobApplications
            .Where(application => applicationIds.Contains(application.Id))
            .Include(application => application.JobPost)
            .ToListAsync();

        if (applications.Count != applicationIds.Count ||
            applications.Any(application => application.Status != JobApplicationStatus.Accepted))
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_BATCH_INVALID",
                "Tất cả hồ sơ được chọn phải đang ở trạng thái Đã duyệt.",
                nameof(request.ApplicationIds));
        }

        // Query database không đảm bảo giữ thứ tự applicationIds từ request.
        // Dictionary giúp lấy nhanh application theo ID, nhưng vẫn xử lý theo đúng thứ tự Admin đã chọn.
        var applicationsById = applications.ToDictionary(application => application.Id);
        var results = new List<Response.InterviewInvitationResultResponse>(applicationIds.Count);

        foreach (var applicationId in applicationIds)
        {
            var application = applicationsById[applicationId];

            var deliveryResult = await _mailService.SendInterviewInvitationAsync(new MailService.InterviewInvitationMailContent
            {
                ApplicationId = application.Id,
                To = application.Email,
                ToName = application.FullName,
                PositionTitle = application.JobPostSnapshot?.Title ?? application.JobPost.Title,
                InterviewAt = interviewAt,
                IdempotencyKey = $"interview-invitation-{application.Id}"
            });

            if (!deliveryResult.IsSuccess)
            {
                results.Add(CreateMailFailedResult(application));
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            application.InterViewAt = interviewAt.ToUniversalTime();
            application.Status = JobApplicationStatus.SendedEmail;
            application.UpdateAt = now;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Interview invitation was sent but could not be persisted. ApplicationId: {ApplicationId}", application.Id);
                throw new JobApplicationException(
                    "JOB_APPLICATION_INTERVIEW_PERSIST_FAILED",
                    "Email đã được gửi nhưng không thể cập nhật hồ sơ ứng viên.",
                    exception);
            }

            results.Add(new Response.InterviewInvitationResultResponse
            {
                ApplicationId = application.Id,
                Outcome = "Sent",
                Status = application.Status.ToString(),
                InterviewAt = interviewAt
            });
        }

        return new Response.SendInterviewInvitationsResponse
        {
            InterviewAt = interviewAt,
            TotalRequested = applicationIds.Count,
            SentCount = results.Count(result => result.Outcome == "Sent"),
            FailedCount = results.Count(result => result.Outcome == "MailFailed"),
            Results = results
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

    private static List<Guid> ValidateApplicationIds(List<Guid>? applicationIds)
    {
        if (applicationIds is null || applicationIds.Count == 0 || applicationIds.Any(id => id == Guid.Empty))
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_REQUEST_INVALID",
                "Danh sách hồ sơ ứng viên không hợp lệ.",
                "applicationIds");
        }

        if (applicationIds.Distinct().Count() != applicationIds.Count)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_REQUEST_INVALID",
                "Danh sách hồ sơ ứng viên không được chứa phần tử trùng lặp.",
                "applicationIds");
        }

        return applicationIds;
    }

    private static DateTimeOffset ParseInterviewAt(string? interviewDate, string? interviewTime)
    {
        if (!DateOnly.TryParseExact(
                interviewDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date) ||
            !TimeOnly.TryParseExact(
                interviewTime,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var time))
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_REQUEST_INVALID",
                "Ngày hoặc giờ phỏng vấn không đúng định dạng.",
                "interviewDate",
                "interviewTime");
        }

        return new DateTimeOffset(date.ToDateTime(time), VietnamOffset);
    }

    private static Response.InterviewInvitationResultResponse CreateMailFailedResult(JobApplication application)
    {
        return new Response.InterviewInvitationResultResponse
        {
            ApplicationId = application.Id,
            Outcome = "MailFailed",
            Status = application.Status.ToString(),
            InterviewAt = application.InterViewAt
        };
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
            GraduationYear = application.GraduationYear,
            Availability = application.Availability,
            AvailableStartDate = application.AvailableStartDate,
            ReferralSource = application.ReferralSource,
            CvUrl = application.CvUrl,
            PortfolioUrl = application.PortfolioUrl,
            CoverLetter = application.CoverLetter,
            JobPostSnapshot = application.JobPostSnapshot,
            Status = GetDisplayName(application.Status),
            ReviewAt = application.ReviewAt,
            ReviewedBy = application.ReviewedBy,
            InterviewAt = application.InterViewAt,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdateAt
        };
    }
}
