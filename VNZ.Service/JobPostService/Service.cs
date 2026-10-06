using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Repository.Entity.Json;
using VNZ.Service.Exceptions;
using VNZ.Service.Localization;

namespace VNZ.Service.JobPostService;

public class Service : IService
{
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;
    private readonly VNZ.Service.Utils.RichTextService.IService _richTextService;

    public Service(AppDbContext dbContext)
        : this(dbContext, new VNZ.Service.Utils.RichTextService.Service())
    {
    }

    public Service(
        AppDbContext dbContext,
        VNZ.Service.Utils.RichTextService.IService richTextService)
    {
        _dbContext = dbContext;
        _richTextService = richTextService;
    }

    public async Task<Response.DeleteJobPostResponse> DeleteJobPostAsync(Guid id)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var affectedRows = await _dbContext.JobPosts
            .Where(jobPost => jobPost.Id == id &&
                              !jobPost.IsDelete &&
                              !(jobPost.Status == JobPostStatus.Open &&
                                jobPost.ExpiredAt.HasValue &&
                                jobPost.ExpiredAt.Value > nowUtc))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(jobPost => jobPost.IsDelete, true));

        if (affectedRows == 1)
        {
            return new Response.DeleteJobPostResponse { Id = id };
        }

        var jobPostExists = await _dbContext.JobPosts
            .AsNoTracking()
            .AnyAsync(jobPost => jobPost.Id == id && !jobPost.IsDelete);

        if (!jobPostExists)
        {
            throw new JobPostException("JOB_POST_NOT_FOUND", "Không tìm thấy tin tuyển dụng.");
        }

        throw new JobPostException(
            "JOB_POST_DELETE_FORBIDDEN",
            "Không thể xóa tin tuyển dụng đang hiển thị trên website.");
    }

    public async Task<Response.CreateJobPostResponse> CreateJobPostAsync(
        Request.CreateJobPostRequest request,
        Guid createdBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Action.HasValue)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_ACTION",
                "Hành động tạo tin không được để trống.",
                "action");
        }

        if (request.Action != JobPostAction.SavedDraft &&
            request.Action != JobPostAction.Publish)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_ACTION",
                "Hành động tạo tin không hợp lệ.",
                "action");
        }
        
        var title = NormalizePlainText(request.Title, "title");
        var translations = SanitizeJobPostTranslations(request.Translations);
        var english = translations?.En;

        if (title is not null && title.Length > 300)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_FAILED",
                "Tiêu đề tin tuyển dụng không hợp lệ.",
                "title");
        }

        var shortDescription = NormalizePlainText(request.ShortDescription, "shortDescription");
        var skills = NormalizeSkills(request.Skills);
        var description = SanitizeRichText(request.Description, "description");
        var requirements = SanitizeRichText(request.Requirements, "requirements");
        var isPublishing = request.Action == JobPostAction.Publish;
        var expiredAt = ConvertExpiredDateToUtc(request.ExpiredDate);
        var nowUtc = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();

        if (request.NumberOfPositions.HasValue && request.NumberOfPositions.Value < 1)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_FAILED",
                "Số lượng vị trí phải lớn hơn 0.",
                "numberOfPositions");
        }

        if (isPublishing)
        {
            var requiredFields = new List<string>();

            if (!request.DepartmentId.HasValue)
            {
                requiredFields.Add("departmentId");
            }

            if (!request.EmploymentType.HasValue)
            {
                requiredFields.Add("employmentType");
            }

            if (!request.JobLevel.HasValue)
            {
                requiredFields.Add("jobLevel");
            }

            if (!request.NumberOfPositions.HasValue || request.NumberOfPositions < 1)
            {
                requiredFields.Add("numberOfPositions");
            }


            if (string.IsNullOrWhiteSpace(shortDescription))

            {
                requiredFields.Add("shortDescription");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                requiredFields.Add("description");
            }

            if (string.IsNullOrWhiteSpace(requirements))
            {
                requiredFields.Add("requirements");
            }

            AddRequiredFieldIfMissing(requiredFields, title, "title");

            if (string.IsNullOrWhiteSpace(english?.Title))
            {
                requiredFields.Add("translations.en.title");
            }

            if (string.IsNullOrWhiteSpace(english?.ShortDescription))
            {
                requiredFields.Add("translations.en.shortDescription");
            }

            if (string.IsNullOrWhiteSpace(english?.Description))
            {
                requiredFields.Add("translations.en.description");
            }

            if (string.IsNullOrWhiteSpace(english?.Requirements))
            {
                requiredFields.Add("translations.en.requirements");
            }

            if (requiredFields.Count > 0)
            {
                throw new JobPostException(
                    requiredFields.Any(field => field.StartsWith("translations.en.", StringComparison.Ordinal))
                        ? "BILINGUAL_CONTENT_REQUIRED"
                        : "JOB_POST_VALIDATION_FAILED",
                    "Vui lòng nhập đầy đủ thông tin để đăng tin.",
                    requiredFields.ToArray());
            }

            if (!expiredAt.HasValue || expiredAt.Value <= nowUtc)
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_EXPIRY",
                    "Ngày hết hạn phải sau thời điểm hiện tại.",
                    "expiredDate");
            }
        }

        if (request.DepartmentId.HasValue)
        {
            var departmentExists = await _dbContext.Departments
                .AnyAsync(department => department.Id == request.DepartmentId.Value);

            if (!departmentExists)
            {
                throw new JobPostException(
                    "DEPARTMENT_NOT_FOUND",
                    "Phòng ban không tồn tại.",
                    "departmentId");
            }
        }
        var jobPost = new JobPost
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            Status = isPublishing ? JobPostStatus.Open : JobPostStatus.Draft,
            ExpiredAt = expiredAt,
            CreatedAt = nowUtc,
            Title = title,
            DepartmentId = request.DepartmentId,
            EmploymentType = request.EmploymentType,
            JobLevel = request.JobLevel,
            NumberOfPositions = request.NumberOfPositions ?? 0,
            Skills = skills,
            ShortDescription = shortDescription,
            Description = description,
            Requirements = requirements,
            Translations = translations   
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        _dbContext.JobPosts.Add(jobPost);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new Response.CreateJobPostResponse
        {
            Id = jobPost.Id,
            Status = GetDisplayName(jobPost.Status),
            ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt),
            CreatedBy = createdBy,
            CreatedAt = jobPost.CreatedAt,
            IsPubliclyVisible = isPublishing
        };
    }

    public async Task<Response.PagedJobPostListResponse> GetJobPostListAsync(
        Request.GetJobPostListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Validate phân trang.
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

            throw new JobPostException(
                "JOB_POST_INVALID_PAGINATION",
                "Thông tin phân trang không hợp lệ.",
                fields.ToArray());
        }

        // 2. Chuẩn hóa và validate từ khóa tìm kiếm.
        var search = request.Search?.Trim();

        if (search is { Length: > 300 })
        {
            throw new JobPostException(
                "JOB_POST_INVALID_SEARCH",
                "Từ khóa tìm kiếm không được vượt quá 300 ký tự.",
                "search");
        }

        // 3. Parse các filter multi-select trước khi query database.
        var statusFilters = new List<JobPostStatus>();
        var statuses = request.Status?
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Select(status => status.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        foreach (var status in statuses)
        {
            var isValidStatus = Enum.TryParse<JobPostStatus>(
                status,
                ignoreCase: false,
                out var parsedStatus);

            if (!isValidStatus || !Enum.IsDefined(parsedStatus))
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_STATUS_FILTER",
                    "Trạng thái lọc không hợp lệ.",
                    "status");
            }

            if (!statusFilters.Contains(parsedStatus))
            {
                statusFilters.Add(parsedStatus);
            }
        }

        var departmentIds = new List<Guid>();
        var departmentIdValues = request.DepartmentId?
            .Where(departmentId => !string.IsNullOrWhiteSpace(departmentId))
            .Select(departmentId => departmentId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        foreach (var departmentIdValue in departmentIdValues)
        {
            var isValidDepartmentId = Guid.TryParse(departmentIdValue, out var departmentId);

            if (!isValidDepartmentId || departmentId == Guid.Empty)
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_DEPARTMENT_FILTER",
                    "Phòng ban lọc không hợp lệ.",
                    "departmentId");
            }

            if (!departmentIds.Contains(departmentId))
            {
                departmentIds.Add(departmentId);
            }
        }

        var jobLevelFilters = new List<JobLevel>();
        var jobLevels = request.JobLevel?
            .Where(jobLevel => !string.IsNullOrWhiteSpace(jobLevel))
            .Select(jobLevel => jobLevel.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];

        foreach (var jobLevel in jobLevels)
        {
            var isValidJobLevel = Enum.TryParse<JobLevel>(
                jobLevel,
                ignoreCase: false,
                out var parsedJobLevel);

            if (!isValidJobLevel || !Enum.IsDefined(parsedJobLevel))
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_JOB_LEVEL_FILTER",
                    "Cấp bậc lọc không hợp lệ.",
                    "jobLevel");
            }

            if (!jobLevelFilters.Contains(parsedJobLevel))
            {
                jobLevelFilters.Add(parsedJobLevel);
            }
        }

        // 4. Bắt đầu query danh sách JobPost.
        var jobPostsQuery = _dbContext.JobPosts.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();

            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                (jobPost.Title ?? string.Empty).ToLower().Contains(searchLower));
        }

        if (statusFilters.Count > 0)
        {
            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                statusFilters.Contains(jobPost.Status));
        }

        if (departmentIds.Count > 0)
        {
            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                jobPost.DepartmentId.HasValue &&
                departmentIds.Contains(jobPost.DepartmentId.Value));
        }

        if (jobLevelFilters.Count > 0)
        {
            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                jobPost.JobLevel.HasValue &&
                jobLevelFilters.Contains(jobPost.JobLevel.Value));
        }

        // 5. Đếm tổng sau filter, sau đó lấy đúng trang cần xem.
        var total = await jobPostsQuery.CountAsync();

        var jobPosts = await jobPostsQuery
            .OrderByDescending(jobPost => jobPost.CreatedAt)
            .ThenByDescending(jobPost => jobPost.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(jobPost => new
            {
                jobPost.Id,
                jobPost.Title,
                jobPost.ShortDescription,
                jobPost.ExpiredAt,
                jobPost.NumberOfPositions,
                jobPost.Status,
                PendingApplicationCount = jobPost.Applications.Count(application =>
                    application.Status == JobApplicationStatus.Pending)
            })
            .ToListAsync();

        // 6. Map dữ liệu database sang response API.
        var items = jobPosts
            .Select(jobPost => new Response.JobPostListItemResponse
            {
                Id = jobPost.Id,
                Title = jobPost.Title,
                ShortDescription = jobPost.ShortDescription,
                ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt),
                NumberOfPositions = jobPost.NumberOfPositions,
                Status = GetDisplayName(jobPost.Status),
                PendingApplicationCount = jobPost.PendingApplicationCount,
                CanDelete = !(jobPost.Status == JobPostStatus.Open &&
                              jobPost.ExpiredAt.HasValue &&
                              jobPost.ExpiredAt.Value > DateTimeOffset.UtcNow),
                DeleteBlockedReason = jobPost.Status == JobPostStatus.Open &&
                                      jobPost.ExpiredAt.HasValue &&
                                      jobPost.ExpiredAt.Value > DateTimeOffset.UtcNow
                    ? "PUBLIC_VISIBLE"
                    : null
            })
            .ToList();

        var totalPages = total == 0
            ? 0
            : (int)Math.Ceiling(total / (double)request.PageSize);

        return new Response.PagedJobPostListResponse
        {
            Items = items,
            Page = request.Page,
            PageSize = request.PageSize,
            Total = total,
            TotalPages = totalPages
        };
    }

    public async Task<List<Response.PublicJobPostListItemResponse>> GetPublicJobPostListAsync(string? lang = null)
    {
        var resolvedLang = LocaleResolver.Resolve(lang);
        var nowUtc = DateTimeOffset.UtcNow;

        try
        {
            var jobPosts = await _dbContext.JobPosts
                .AsNoTracking()
                .Include(jobPost => jobPost.Department)
                .Where(jobPost => jobPost.Status == JobPostStatus.Open &&
                                  jobPost.ExpiredAt.HasValue &&
                                  jobPost.ExpiredAt.Value > nowUtc)
                .OrderBy(jobPost => jobPost.ExpiredAt)
                .ThenByDescending(jobPost => jobPost.CreatedAt)
                .ThenByDescending(jobPost => jobPost.Id)
                .ToListAsync();

            if (resolvedLang == LocaleResolver.English)
            {
                foreach (var jobPost in jobPosts)
                {
                    RequireJobPostVietnamese(
                        jobPost.Title,
                        jobPost.ShortDescription,
                        jobPost.Description,
                        jobPost.Requirements);
                }
            }

            return jobPosts
                .Select(jobPost => new Response.PublicJobPostListItemResponse
                {
                    Id = jobPost.Id,
                    Title = resolvedLang == LocaleResolver.English
                        ? RequireJobPostEnglish(jobPost.Translations).Title!
                        : jobPost.Title!,
                    Department = jobPost.Department?.Code,
                    EmploymentType = jobPost.EmploymentType.HasValue
                        ? jobPost.EmploymentType.Value.ToString()
                        : null,
                    JobLevel = jobPost.JobLevel.HasValue
                        ? jobPost.JobLevel.Value.ToString()
                        : null,
                    NumberOfPositions = jobPost.NumberOfPositions,
                    Skills = new List<string>(jobPost.Skills),
                    ShortDescription = resolvedLang == LocaleResolver.English
                        ? RequireJobPostEnglish(jobPost.Translations).ShortDescription!
                        : jobPost.ShortDescription,
                    ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt)!.Value
                })
                .ToList();
        }
        catch (LocalizationException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Anh của vị trí tuyển dụng không hợp lệ.",
                new[] { "translations.en" });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new JobPostException(
                "PUBLIC_JOB_POST_LIST_READ_FAILED",
                "Không thể lấy danh sách vị trí tuyển dụng.",
                exception);
        }
    }

    public async Task<Response.JobPostDetailResponse> GetJobPostDetailAsync(Guid id)
    {
        // 1. Đọc JobPost hiện tại cùng dữ liệu hiển thị liên quan.
        var jobPost = await _dbContext.JobPosts
            .AsNoTracking()
            .Include(jobPost => jobPost.Department)
            .Include(jobPost => jobPost.Creator)
            .FirstOrDefaultAsync(jobPost => jobPost.Id == id);

        // 2. Không tìm thấy thì không trả dữ liệu detail.
        if (jobPost is null)
        {
            throw new JobPostException(
                "JOB_POST_NOT_FOUND",
                "Không tìm thấy tin tuyển dụng.");
        }

        // 3. Draft luôn có thể sửa. Open chỉ sửa được khi chưa đến hạn.
        var canEdit = jobPost.Status == JobPostStatus.Draft ||
            (jobPost.Status == JobPostStatus.Open &&
             jobPost.ExpiredAt.HasValue &&
             jobPost.ExpiredAt.Value > DateTimeOffset.UtcNow);

        // 4. Map dữ liệu database sang response API.
        return new Response.JobPostDetailResponse
        {
            Id = jobPost.Id,
            Title = jobPost.Title,
            CreatedByName = jobPost.Creator?.FullName,
            UpdatedAt = jobPost.UpdatedAt,
            Status = GetDisplayName(jobPost.Status),
            ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt),
            Department = jobPost.Department?.Code,
            EmploymentType = jobPost.EmploymentType.HasValue
                ? GetDisplayName(jobPost.EmploymentType.Value)
                : null,
            JobLevel = jobPost.JobLevel.HasValue
                ? GetDisplayName(jobPost.JobLevel.Value)
                : null,
            NumberOfPositions = jobPost.NumberOfPositions,
            Skills = new List<string>(jobPost.Skills),
            ShortDescription = jobPost.ShortDescription,
            Description = jobPost.Description,
            Requirements = jobPost.Requirements,
            CanEdit = canEdit,
            Translations = ToTranslationsResponse(jobPost.Translations),

            CanDelete = !(jobPost.Status == JobPostStatus.Open &&
                          jobPost.ExpiredAt.HasValue &&
                          jobPost.ExpiredAt.Value > DateTimeOffset.UtcNow),
            DeleteBlockedReason = jobPost.Status == JobPostStatus.Open &&
                                  jobPost.ExpiredAt.HasValue &&
                                  jobPost.ExpiredAt.Value > DateTimeOffset.UtcNow
                ? "PUBLIC_VISIBLE"
                : null

        };
    }

    public async Task<Response.PublicJobPostDetailResponse> GetPublicJobPostDetailAsync(
        Guid id,
        string? lang = null)
    {
        var resolvedLang = LocaleResolver.Resolve(lang);
        var nowUtc = DateTimeOffset.UtcNow;

        try
        {
            var jobPost = await _dbContext.JobPosts
                .AsNoTracking()
                .Where(jobPost => jobPost.Id == id &&
                                  jobPost.Status == JobPostStatus.Open &&
                                  jobPost.ExpiredAt.HasValue &&
                                  jobPost.ExpiredAt.Value > nowUtc)
                .Select(jobPost => new
                {
                    jobPost.Id,
                    jobPost.Title,
                    Department = jobPost.Department == null
                        ? null
                        : jobPost.Department.Code,
                    jobPost.EmploymentType,
                    jobPost.JobLevel,
                    jobPost.NumberOfPositions,
                    jobPost.Skills,
                    jobPost.ShortDescription,
                    jobPost.Description,
                    jobPost.Requirements,
                    jobPost.Translations,
                    jobPost.ExpiredAt
                })
                .SingleOrDefaultAsync();

            if (jobPost is null)
            {
                throw new JobPostException(
                    "PUBLIC_JOB_POST_NOT_AVAILABLE",
                    "Vị trí tuyển dụng không còn mở. Vui lòng xem danh sách vị trí đang tuyển.",
                    "id");
            }

            if (resolvedLang == LocaleResolver.English)
            {
                RequireJobPostVietnamese(
                    jobPost.Title,
                    jobPost.ShortDescription,
                    jobPost.Description,
                    jobPost.Requirements);
            }

            return new Response.PublicJobPostDetailResponse
            {
                Id = jobPost.Id,
                Title = resolvedLang == LocaleResolver.English
                    ? RequireJobPostEnglish(jobPost.Translations).Title!
                    : jobPost.Title!,
                Department = jobPost.Department,
                EmploymentType = jobPost.EmploymentType.HasValue
                    ? jobPost.EmploymentType.Value.ToString()
                    : null,
                JobLevel = jobPost.JobLevel.HasValue
                    ? jobPost.JobLevel.Value.ToString()
                    : null,
                NumberOfPositions = jobPost.NumberOfPositions,
                Skills = new List<string>(jobPost.Skills),
                ShortDescription = resolvedLang == LocaleResolver.English
                    ? RequireJobPostEnglish(jobPost.Translations).ShortDescription!
                    : jobPost.ShortDescription,
                Description = resolvedLang == LocaleResolver.English
                    ? RequireJobPostEnglish(jobPost.Translations).Description!
                    : jobPost.Description,
                Requirements = resolvedLang == LocaleResolver.English
                    ? RequireJobPostEnglish(jobPost.Translations).Requirements!
                    : jobPost.Requirements,
                ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt)!.Value
            };
        }
        catch (LocalizationException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Anh của vị trí tuyển dụng không hợp lệ.",
                new[] { "translations.en" });
        }
        catch (JobPostException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new JobPostException(
                "PUBLIC_JOB_POST_DETAIL_READ_FAILED",
                "Không thể lấy chi tiết vị trí tuyển dụng.",
                exception);
        }
    }

    public async Task<Response.UpdateJobPostResponse> UpdateJobPostAsync(
        Guid id,
        Request.UpdateJobPostRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Action.HasValue)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_ACTION",
                "Hành động cập nhật tin không được để trống.",
                "action");
        }

        var jobPost = await _dbContext.JobPosts
            .Include(item => item.Department)
            .Include(item => item.Creator)
            .SingleOrDefaultAsync(item => item.Id == id);

        if (jobPost is null)
        {
            throw new JobPostException(
                "JOB_POST_NOT_FOUND",
                "Không tìm thấy tin tuyển dụng.");
        }

        EnsureExpectedUpdatedAt(request.ExpectedUpdatedAt, jobPost.UpdatedAt);

        if (jobPost.Status == JobPostStatus.Closed)
        {
            throw new JobPostException(
                "JOB_POST_CLOSED",
                "Tin tuyển dụng đã đóng, không được chỉnh sửa.");
        }

        if (jobPost.Status == JobPostStatus.Expired)
        {
            throw new JobPostException(
                "JOB_POST_EXPIRED",
                "Tin tuyển dụng đã hết hạn, không được chỉnh sửa.");
        }

        var nowUtc = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();
        var expiredAt = ConvertExpiredDateToUtc(request.ExpiredDate);

        if (jobPost.Status == JobPostStatus.Open &&
            jobPost.ExpiredAt.HasValue &&
            jobPost.ExpiredAt.Value <= nowUtc)
        {
            throw new JobPostException(
                "JOB_POST_EXPIRED",
                "Tin tuyển dụng đã hết hạn, không được chỉnh sửa.");
        }

        if (request.Action == JobPostAction.Close)
        {
            if (jobPost.Status != JobPostStatus.Open)
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_ACTION",
                    "Chỉ tin đang tuyển mới có thể đóng.",
                    "action");
            }

            jobPost.Status = JobPostStatus.Closed;
            jobPost.UpdatedAt = nowUtc;

            await SaveJobPostUpdateAsync();
            return ToUpdateResponse(jobPost);
        }

        if (request.Action == JobPostAction.SavedDraft &&
            jobPost.Status != JobPostStatus.Draft)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_ACTION",
                "Chỉ tin bản nháp mới có thể được lưu dưới dạng bản nháp.",
                "action");
        }

        var title = NormalizePlainText(request.Title, "title");
        var translations = SanitizeJobPostTranslations(request.Translations);
        var english = translations?.En;

        if (request.NumberOfPositions.HasValue && request.NumberOfPositions.Value < 1)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_ERROR",
                "Số lượng vị trí phải lớn hơn 0.",
                "numberOfPositions");
        }

        if (title is not null && title.Length > 300)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_ERROR",
                "Tiêu đề tin tuyển dụng không hợp lệ.",
                "title");
        }

        var shortDescription = NormalizePlainText(request.ShortDescription, "shortDescription");
        var skills = NormalizeSkills(request.Skills);
        var description = SanitizeRichText(request.Description, "description");
        var requirements = SanitizeRichText(request.Requirements, "requirements");
        var department = await FindDepartmentAsync(request.DepartmentId);

        if (request.Action == JobPostAction.Publish)
        {
            var requiredFields = new List<string>();

            if (!request.DepartmentId.HasValue)
            {
                requiredFields.Add("departmentId");
            }

            if (!request.EmploymentType.HasValue)
            {
                requiredFields.Add("employmentType");
            }

            if (!request.JobLevel.HasValue)
            {
                requiredFields.Add("jobLevel");
            }

            if (!request.NumberOfPositions.HasValue || request.NumberOfPositions < 1)
            {
                requiredFields.Add("numberOfPositions");
            }


            if (string.IsNullOrWhiteSpace(shortDescription))

            {
                requiredFields.Add("shortDescription");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                requiredFields.Add("description");
            }

            if (string.IsNullOrWhiteSpace(requirements))
            {
                requiredFields.Add("requirements");
            }

            if (!expiredAt.HasValue || expiredAt.Value <= nowUtc)
            {
                requiredFields.Add("expiredDate");
            }

            AddRequiredFieldIfMissing(requiredFields, title, "title");
            AddRequiredFieldIfMissing(requiredFields, english?.Title, "translations.en.title");
            AddRequiredFieldIfMissing(requiredFields, english?.ShortDescription, "translations.en.shortDescription");
            AddRequiredFieldIfMissing(requiredFields, english?.Description, "translations.en.description");
            AddRequiredFieldIfMissing(requiredFields, english?.Requirements, "translations.en.requirements");

            if (requiredFields.Count > 0)
            {
                throw new JobPostException(
                    requiredFields.Any(field => field.StartsWith("translations.en.", StringComparison.Ordinal))
                        ? "BILINGUAL_CONTENT_REQUIRED"
                        : "JOB_POST_VALIDATION_ERROR",
                    "Vui lòng nhập đầy đủ thông tin để lưu tin đang tuyển.",
                    requiredFields.ToArray());
            }
        }

        if (request.Action != JobPostAction.SavedDraft &&
            request.Action != JobPostAction.Publish)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_ACTION",
                "Hành động cập nhật tin không hợp lệ.",
                "action");
        }

        jobPost.Title = title;
        jobPost.DepartmentId = request.DepartmentId;
        jobPost.Department = department;
        jobPost.EmploymentType = request.EmploymentType;
        jobPost.JobLevel = request.JobLevel;
        jobPost.NumberOfPositions = request.NumberOfPositions ?? 0;
        jobPost.Skills = skills;
        jobPost.ShortDescription = shortDescription;
        jobPost.Description = description;
        jobPost.Requirements = requirements;
        jobPost.ExpiredAt = expiredAt;
        jobPost.Status = request.Action == JobPostAction.Publish
            ? JobPostStatus.Open
            : JobPostStatus.Draft;
        jobPost.UpdatedAt = nowUtc;
        jobPost.Translations = translations;

        await SaveJobPostUpdateAsync();
        return ToUpdateResponse(jobPost);
    }

    private async Task<Department?> FindDepartmentAsync(Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return null;
        }

        var department = await _dbContext.Departments
            .SingleOrDefaultAsync(item => item.Id == departmentId.Value);

        if (department is null)
        {
            throw new JobPostException(
                "DEPARTMENT_NOT_FOUND",
                "Phòng ban không tồn tại.",
                "departmentId");
        }

        return department;
    }

    private string? NormalizePlainText(string? value, string field)
    {
        if (_richTextService.ContainsHtmlTag(value))
        {
            throw new JobPostException(
                "JOB_POST_INVALID_RICH_TEXT",
                "Trường này chỉ nhận plain text.",
                field);
        }

        return value?.Trim();
    }

    private List<string> NormalizeSkills(List<string>? skills)
    {
        if (skills is null)
        {
            return new List<string>();
        }

        var normalizedSkills = skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => NormalizePlainText(skill, "skills")!)
            .ToList();

        return normalizedSkills;
    }

    private string? SanitizeRichText(string? value, string field)
    {
        try
        {
            return _richTextService.SanitizeJobPost(value);
        }
        catch (VNZ.Service.Utils.RichTextService.RichTextValidationException exception)
        {
            throw new JobPostException(
                "JOB_POST_INVALID_RICH_TEXT",
                exception.Message,
                field);
        }
    }

    private static JobPostTranslations? SanitizeJobPostTranslations(
        Request.JobPostTranslationsRequest? translations)
    {
        if (translations is null)
        {
            return null;
        }

        if (translations.En is null)
        {
            return new JobPostTranslations();
        }

        var english = translations.En;
        var title = english.Title?.Trim();

        if (title is not null && title.Length > 300)
        {
            throw new JobPostException(
                "BILINGUAL_SCHEMA_INVALID",
                "Cấu trúc nội dung tiếng Anh không hợp lệ.",
                "translations.en.title");
        }

        return new JobPostTranslations
        {
            En = new JobPostEnglishTranslation
            {
                Title = title,
                ShortDescription = english.ShortDescription?.Trim(),
                Description = english.Description?.Trim(),
                Requirements = english.Requirements?.Trim()
            }
        };
    }

    private static void AddRequiredFieldIfMissing(
        ICollection<string> fields,
        string? value,
        string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            fields.Add(field);
        }
    }

    private static void EnsureExpectedUpdatedAt(
        DateTimeOffset? expectedUpdatedAt,
        DateTimeOffset? actualUpdatedAt)
    {
        if (expectedUpdatedAt.HasValue && expectedUpdatedAt != actualUpdatedAt)
        {
            throw new JobPostException(
                "CONTENT_CONFLICT",
                "Tin tuyển dụng đã được cập nhật bởi một yêu cầu khác.",
                "expectedUpdatedAt");
        }
    }

    private static JobPostEnglishTranslation RequireJobPostEnglish(
        JobPostTranslations? translations)
    {
        var english = translations?.En;
        var missingFields = new List<string>();

        AddRequiredFieldIfMissing(missingFields, english?.Title, "translations.en.title");
        AddRequiredFieldIfMissing(missingFields, english?.ShortDescription, "translations.en.shortDescription");
        AddRequiredFieldIfMissing(missingFields, english?.Description, "translations.en.description");
        AddRequiredFieldIfMissing(missingFields, english?.Requirements, "translations.en.requirements");

        if (missingFields.Count > 0)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Anh của vị trí tuyển dụng đang bị thiếu.",
                missingFields.ToArray());
        }

        return english!;
    }

    private static void RequireJobPostVietnamese(
        string? title,
        string? shortDescription,
        string? description,
        string? requirements)
    {
        var missingFields = new List<string>();

        AddRequiredFieldIfMissing(missingFields, title, "title");
        AddRequiredFieldIfMissing(missingFields, shortDescription, "shortDescription");
        AddRequiredFieldIfMissing(missingFields, description, "description");
        AddRequiredFieldIfMissing(missingFields, requirements, "requirements");

        if (missingFields.Count > 0)
        {
            throw new LocalizationException(
                "PUBLIC_TRANSLATION_MISSING",
                "Bản dịch tiếng Việt của vị trí tuyển dụng đang bị thiếu.",
                missingFields.ToArray());
        }
    }

    private static DateTimeOffset? ConvertExpiredDateToUtc(DateOnly? expiredDate)
    {
        if (!expiredDate.HasValue)
        {
            return null;
        }

        var nextDate = expiredDate.Value.AddDays(1);
        var nextDateStartInVietnam = nextDate.ToDateTime(TimeOnly.MinValue);
        var expirationTime = new DateTimeOffset(nextDateStartInVietnam, VietnamUtcOffset);

        return expirationTime.ToUniversalTime();
    }

    private static DateOnly? ConvertExpiredAtToDate(DateTimeOffset? expiredAt)
    {
        if (!expiredAt.HasValue)
        {
            return null;
        }

        var expirationTimeInVietnam = expiredAt.Value.ToOffset(VietnamUtcOffset);
        var nextDate = DateOnly.FromDateTime(expirationTimeInVietnam.DateTime);

        return nextDate.AddDays(-1);
    }

    private async Task SaveJobPostUpdateAsync()
    {
        try
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new JobPostException(
                "CONTENT_CONFLICT",
                "Tin tuyển dụng đã được cập nhật bởi một yêu cầu khác.",
                "expectedUpdatedAt");
        }
        catch (DbUpdateException exception)
        {
            throw new JobPostException(
                "JOB_POST_UPDATE_FAILED",
                "Không thể cập nhật tin tuyển dụng.",
                exception);
        }
    }

    private static Response.UpdateJobPostResponse ToUpdateResponse(JobPost jobPost)
    {
        return new Response.UpdateJobPostResponse
        {
            Id = jobPost.Id,
            Title = jobPost.Title,
            CreatedByName = jobPost.Creator?.FullName,
            UpdatedAt = jobPost.UpdatedAt,
            Status = GetDisplayName(jobPost.Status),
            ExpiredDate = ConvertExpiredAtToDate(jobPost.ExpiredAt),
            Department = jobPost.Department?.Code,
            EmploymentType = jobPost.EmploymentType.HasValue
                ? GetDisplayName(jobPost.EmploymentType.Value)
                : null,
            JobLevel = jobPost.JobLevel.HasValue
                ? GetDisplayName(jobPost.JobLevel.Value)
                : null,
            NumberOfPositions = jobPost.NumberOfPositions,
            Skills = new List<string>(jobPost.Skills),
            ShortDescription = jobPost.ShortDescription,
            Description = jobPost.Description,
            Requirements = jobPost.Requirements,
            Translations = ToTranslationsResponse(jobPost.Translations)
        };
    }

    private static Response.JobPostTranslationsResponse? ToTranslationsResponse(
        JobPostTranslations? translations)
    {
        if (translations is null)
        {
            return null;
        }

        return new Response.JobPostTranslationsResponse
        {
            En = translations.En is null
                ? null
                : new Response.JobPostEnglishTranslationResponse
                {
                    Title = translations.En.Title,
                    ShortDescription = translations.En.ShortDescription,
                    Description = translations.En.Description,
                    Requirements = translations.En.Requirements
                }
        };
    }

    private static string GetDisplayName<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        // Enum/system values are stable API keys. FE owns the VI/EN labels.
        return value.ToString();
    }
}
