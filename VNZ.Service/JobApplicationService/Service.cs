using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobApplicationService;

public sealed class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.CreateJobApplicationResponse> CreateAsync(Request.CreateJobApplicationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var fullName = request.FullName?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        var phone = request.Phone?.Trim();
        var cvUrl = request.CvUrl?.Trim();
        var coverLetter = request.CoverLetter?.Trim();

        var requiredFields = new List<string>();
        if (!request.JobPostId.HasValue || request.JobPostId.Value == Guid.Empty)
        {
            requiredFields.Add("jobPostId");
        }

        if (string.IsNullOrWhiteSpace(fullName)) requiredFields.Add("fullName");
        if (string.IsNullOrWhiteSpace(email)) requiredFields.Add("email");
        if (string.IsNullOrWhiteSpace(phone)) requiredFields.Add("phone");
        if (string.IsNullOrWhiteSpace(cvUrl)) requiredFields.Add("cvUrl");
        if (string.IsNullOrWhiteSpace(coverLetter)) requiredFields.Add("coverLetter");

        if (requiredFields.Count > 0)
        {
            throw new JobApplicationException("JOB_APPLICATION_VALIDATION_FAILED", "Vui lòng nhập đầy đủ thông tin bắt buộc.",
                requiredFields.ToArray());
        }

        if (!request.ConsentToDataProcessing)
        {
            throw new JobApplicationException("JOB_APPLICATION_CONSENT_REQUIRED",
                "Ứng viên phải đồng ý để VNZ lưu trữ và xử lý thông tin phục vụ tuyển dụng.",
                "consentToDataProcessing");
        }

        if (fullName!.Length > 200)
        {
            throw new JobApplicationException("JOB_APPLICATION_VALIDATION_FAILED",
                "Họ và tên không được vượt quá 200 ký tự.",
                "fullName");
        }

        if (email!.Length > 320 || !new EmailAddressAttribute().IsValid(email))
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_VALIDATION_FAILED",
                "Email không đúng định dạng.",
                "email");
        }

        if (!IsHttpUrl(cvUrl!))
        {
            throw new JobApplicationException("JOB_APPLICATION_CV_URL_INVALID",
                "Liên kết CV không hợp lệ.",
                "cvUrl");
        }

        var portfolioUrl = NormalizeOptional(request.PortfolioUrl);
        if (portfolioUrl is not null && !IsHttpUrl(portfolioUrl))
        {
            throw new JobApplicationException("JOB_APPLICATION_VALIDATION_FAILED",
                "Liên kết portfolio không hợp lệ.",
                "portfolioUrl");
        }

        var availability = NormalizeOptional(request.Availability);
        var availableStartDate = NormalizeOptional(request.AvailableStartDate);
        var referralSource = NormalizeOptional(request.ReferralSource);

        if (availability?.Length > 100 || availableStartDate?.Length > 100 || referralSource?.Length > 200)
        {
            throw new JobApplicationException("JOB_APPLICATION_VALIDATION_FAILED",
                "Thông tin bổ sung vượt quá độ dài cho phép.",
                availability?.Length > 100 ? "availability" : availableStartDate?.Length > 100 ? "availableStartDate" : "referralSource");
        }

        var jobPost = await _dbContext.JobPosts
            .Include(item => item.Department)
            .SingleOrDefaultAsync(item => item.Id == request.JobPostId!.Value);

        if (jobPost is null)
        {
            throw new JobApplicationException(
                "JOB_POST_NOT_FOUND",
                "Không tìm thấy vị trí tuyển dụng.",
                "jobPostId");
        }

        var now = DateTimeOffset.UtcNow;
        if (jobPost.Status != JobPostStatus.Open || !jobPost.ExpiredAt.HasValue || jobPost.ExpiredAt <= now)
        {
            throw new JobApplicationException("JOB_POST_NOT_AVAILABLE",
                "Vị trí tuyển dụng không còn nhận hồ sơ.",
                "jobPostId");
        }

        if (!jobPost.DepartmentId.HasValue || jobPost.Department is null ||
            !jobPost.EmploymentType.HasValue || !jobPost.JobLevel.HasValue)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_CREATE_FAILED",
                "Không thể tạo hồ sơ ứng tuyển do dữ liệu vị trí tuyển dụng không đầy đủ.");
        }

        var application = new JobApplication
        {
            Id = Guid.NewGuid(),
            JobPostId = jobPost.Id,
            FullName = fullName,
            Email = email,
            Phone = phone,
            GraduationYear = request.GraduationYear,
            University = NormalizeOptional(request.University),
            Major = NormalizeOptional(request.Major),
            CvUrl = cvUrl,
            PortfolioUrl = portfolioUrl,
            CoverLetter = coverLetter,
            Availability = availability,
            AvailableStartDate = availableStartDate,
            ReferralSource = referralSource,
            ConsentToDataProcessing = true,
            JobPostSnapshot = CreateJobPostSnapshot(jobPost),
            Status = JobApplicationStatus.Pending,
            CreatedAt = now,
            UpdateAt = now
        };

        try
        {
            _dbContext.JobApplications.Add(application);
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_CREATE_FAILED",
                "Không thể tạo hồ sơ ứng tuyển.",
                exception);
        }

        return new Response.CreateJobApplicationResponse
        {
            Id = application.Id,
            JobPostId = application.JobPostId,
            JobPostTitle = jobPost.Title,
            FullName = application.FullName,
            Email = application.Email,
            Phone = application.Phone!,
            GraduationYear = application.GraduationYear,
            University = application.University,
            Major = application.Major,
            CvUrl = application.CvUrl!,
            PortfolioUrl = application.PortfolioUrl,
            CoverLetter = application.CoverLetter!,
            Availability = application.Availability,
            AvailableStartDate = application.AvailableStartDate,
            ReferralSource = application.ReferralSource,
            ConsentToDataProcessing = application.ConsentToDataProcessing,
            Status = GetStatusLabel(application.Status),
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdateAt,
            ReviewAt = application.ReviewAt,
            ReviewedBy = application.ReviewedBy,
            InterviewAt = application.InterViewAt,
            JobPostSnapshot = application.JobPostSnapshot
        };
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
            GraduationYear = application.GraduationYear,
            Availability = application.Availability,
            AvailableStartDate = application.AvailableStartDate,
            ReferralSource = application.ReferralSource,
            CvUrl = application.CvUrl,
            PortfolioUrl = application.PortfolioUrl,
            CoverLetter = application.CoverLetter,
            ConsentToDataProcessing = application.ConsentToDataProcessing,
            JobPostSnapshot = application.JobPostSnapshot,
            Status = application.Status.ToString(),
            ReviewAt = application.ReviewAt,
            ReviewedBy = application.ReviewedBy,
            InterviewAt = application.InterViewAt,
            CreatedAt = application.CreatedAt,
            UpdatedAt = application.UpdateAt
        };
    }

    private static JobPostSnapshot CreateJobPostSnapshot(JobPost jobPost)
    {
        return new JobPostSnapshot
        {
            DepartmentId = jobPost.DepartmentId!.Value,
            DepartmentName = jobPost.Department!.Name,
            Title = jobPost.Title,
            EmploymentType = jobPost.EmploymentType!.Value,
            JobLevel = jobPost.JobLevel!.Value,
            NumberOfPositions = jobPost.NumberOfPositions,
            ShortDescription = jobPost.ShortDescription ?? string.Empty,
            Description = jobPost.Description ?? string.Empty,
            Requirements = jobPost.Requirements ?? string.Empty,
            ExpiredAt = jobPost.ExpiredAt,
            JobSkillsSnapshot = new JobSkillsSnapshot
            {
                Skills = jobPost.Skills.ToList()
            }
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
