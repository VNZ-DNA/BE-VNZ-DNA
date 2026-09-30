

using System.Globalization;

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobApplicationService;

public sealed class Service : IService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly MailService.IService _mailService;
    private readonly ILogger<Service> _logger;
    private readonly MailService.IEmailTemplateRenderer _emailTemplateRenderer;
    private readonly VNZ.Service.Utils.RichTextService.IService _richTextService;

    public Service(
        AppDbContext dbContext,
        MailService.IService mailService,
        ILogger<Service> logger)
        : this(
            dbContext,
            mailService,
            logger,
            new MailService.EmailTemplateRenderer(),
            new VNZ.Service.Utils.RichTextService.Service())
    {
    }

    public Service(
        AppDbContext dbContext,
        MailService.IService mailService,
        ILogger<Service> logger,
        MailService.IEmailTemplateRenderer emailTemplateRenderer,
        VNZ.Service.Utils.RichTextService.IService richTextService)
    {
        _dbContext = dbContext;
        _mailService = mailService;
        _logger = logger;
        _emailTemplateRenderer = emailTemplateRenderer;
        _richTextService = richTextService;
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

        if (decision != "accepted" && decision != "rejected")
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_REJECTION_REQUEST_INVALID",
                "Decision chỉ nhận Accepted hoặc Rejected.",
                "decision");
        }

        if (decision == "rejected")
        {
            return await RejectDirectlyAsync(id, adminUserId);
        }

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var application = await _dbContext.JobApplications
            .SingleOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_NOT_FOUND",
                "Không tìm thấy hồ sơ ứng viên.",
                "id");
        }

        if (application.Status != JobApplicationStatus.Pending)
        {
            throw new JobApplicationException(
                "INVALID_APPLICATION_STATUS",
                "Hồ sơ ứng viên không còn ở trạng thái Chờ duyệt.");
        }

        var now = DateTimeOffset.UtcNow;
        application.Status = JobApplicationStatus.Accepted;
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

    private async Task<Response.ReviewJobApplicationResponse> RejectDirectlyAsync(
        Guid id,
        Guid adminUserId)
    {
        var application = await _dbContext.JobApplications
            .Include(item => item.JobPost)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (application is null)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_NOT_FOUND",
                "Không tìm thấy hồ sơ ứng viên.",
                "id");
        }

        if (application.Status != JobApplicationStatus.Pending)
        {
            throw new JobApplicationException(
                "INVALID_APPLICATION_STATUS",
                "Hồ sơ ứng viên không còn ở trạng thái Chờ duyệt.");
        }

        var positionTitle = application.JobPostSnapshot?.Title ?? application.JobPost.Title;
        var deliveryResult = await _mailService.SendRejectionEmailAsync(
            new MailService.RejectionEmailMailContent
            {
                ApplicationId = application.Id,
                To = application.Email,
                ToName = application.FullName,
                PositionTitle = positionTitle,
                IdempotencyKey = $"rejection-mail-{application.Id}"
            });

        if (!deliveryResult.IsSuccess)
        {
            // Mail thất bại thì chưa đụng vào audit hoặc Status; hồ sơ vẫn Pending.
            throw new JobApplicationException(
                "JOB_APPLICATION_REJECTION_EMAIL_FAILED",
                "Không thể gửi email từ chối hồ sơ ứng viên.");
        }

        var now = DateTimeOffset.UtcNow;
        application.Status = JobApplicationStatus.Rejected;
        application.ReviewedBy = adminUserId;
        application.ReviewAt = now;
        application.UpdateAt = now;

        try
        {
            // Đây là failure window của luồng gửi trực tiếp: provider đã nhận mail
            // nhưng DB có thể lỗi, khi đó hồ sơ vẫn Pending dù ứng viên đã nhận mail.
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_REJECTION_PERSIST_FAILED",
                "Email đã gửi nhưng không thể cập nhật hồ sơ ứng viên.",
                exception);
        }

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
            .Include(application => application.JobPost)
            .ToListAsync();

        var applications = applicationRows.Select(application => new Response.JobApplicationListItemResponse
        {
            Id = application.Id,
            FullName = application.FullName,
            Email = application.Email,
            JobPostId = application.JobPostId,
            JobPostTitle = application.JobPost.Title,
            JobPostSnapshotTitle = application.JobPostSnapshot?.Title,
            Status = GetStatusLabel(application.Status),
            CvUrl = application.CvUrl,
            CreatedAt = application.CreatedAt,
            InterviewAt = application.InterViewAt,
            CanSelectForInterviewEmail = application.Status == JobApplicationStatus.Accepted
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

    public Task<VNZ.Service.MailService.Response.InterviewTemplateSchemaResponse> GetInterviewInvitationTemplateAsync()
    {
        return Task.FromResult(VNZ.Service.MailService.EmailTemplateSchemaProvider.GetInterviewSchema());
    }

    public async Task<Response.SendInterviewInvitationsResponse> SendInterviewInvitationsAsync(
        Request.SendInterviewInvitationsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateAdditionalFields(request.AdditionalFields);

        var applicationIds = ValidateApplicationIds(request.ApplicationIds);
        var interviewAt = ParseInterviewAt(request.InterviewDate, request.InterviewTime);
        var normalizedContent = NormalizeInterviewContent(request);

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
                DurationMinutes = normalizedContent.DurationMinutes,
                InterviewMode = normalizedContent.InterviewMode,
                Location = normalizedContent.Location,
                LocationUrl = normalizedContent.LocationUrl,
                InterviewInformationHtml = normalizedContent.InterviewInformationHtml,
                AgendaHtml = normalizedContent.AgendaHtml,
                PreparationHtml = normalizedContent.PreparationHtml,
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

    private NormalizedInterviewContent NormalizeInterviewContent(
        Request.SendInterviewInvitationsRequest request)
    {
        var fields = new List<string>();
        var mode = request.InterviewMode?.Trim();
        var location = NormalizeOptional(request.Location);
        var locationUrl = NormalizeOptional(request.LocationUrl);

        if (!request.DurationMinutes.HasValue || request.DurationMinutes.Value <= 0)
        {
            fields.Add("durationMinutes");
        }

        if (!string.Equals(mode, "Onsite", StringComparison.Ordinal)
            && !string.Equals(mode, "Online", StringComparison.Ordinal))
        {
            fields.Add("interviewMode");
        }

        if (string.Equals(mode, "Onsite", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(location))
        {
            fields.Add("location");
        }

        if (string.IsNullOrWhiteSpace(locationUrl) || !IsHttpsUrl(locationUrl))
        {
            fields.Add("locationUrl");
        }

        var interviewInformationHtml = NormalizeEmailRichText(
            request.InterviewInformationHtml,
            "interviewInformationHtml",
            required: false,
            fields);
        var agendaHtml = NormalizeEmailRichText(
            request.AgendaHtml,
            "agendaHtml",
            required: true,
            fields);
        var preparationHtml = NormalizeEmailRichText(
            request.PreparationHtml,
            "preparationHtml",
            required: true,
            fields);

        if (fields.Count > 0)
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_CONTENT_INVALID",
                "Nội dung email mời phỏng vấn không hợp lệ.",
                fields.Distinct().ToArray());
        }

        return new NormalizedInterviewContent
        {
            DurationMinutes = request.DurationMinutes!.Value,
            InterviewMode = mode!,
            Location = location,
            LocationUrl = locationUrl,
            InterviewInformationHtml = interviewInformationHtml,
            AgendaHtml = agendaHtml!,
            PreparationHtml = preparationHtml!
        };
    }

    private static void ValidateAdditionalFields(
        Dictionary<string, System.Text.Json.JsonElement>? additionalFields)
    {
        if (additionalFields is { Count: > 0 })
        {
            throw new JobApplicationException(
                "JOB_APPLICATION_INTERVIEW_REQUEST_INVALID",
                "Payload chứa field không thuộc contract email mời phỏng vấn.",
                additionalFields.Keys.ToArray());
        }
    }

    private sealed class NormalizedInterviewContent
    {
        public int DurationMinutes { get; init; }
        public string InterviewMode { get; init; } = string.Empty;
        public string? Location { get; init; }
        public string? LocationUrl { get; init; }
        public string? InterviewInformationHtml { get; init; }
        public string AgendaHtml { get; init; } = string.Empty;
        public string PreparationHtml { get; init; } = string.Empty;
    }

    private static string GetStatusLabel<TEnum>(TEnum value)
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
            ConsentToDataProcessing = application.ConsentToDataProcessing,
            JobPostSnapshot = application.JobPostSnapshot,
            Status = GetStatusLabel(application.Status),
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

    private static bool IsHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}

