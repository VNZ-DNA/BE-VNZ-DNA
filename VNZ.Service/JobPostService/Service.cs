using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobPostService;

public class Service : IService
{
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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

        var title = request.Title?.Trim();

        if (string.IsNullOrWhiteSpace(title) || title.Length > 300)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_FAILED",
                "Tiêu đề tin tuyển dụng không hợp lệ.",
                "title");
        }

        var isPublishing = request.Action == JobPostAction.Publish;
        var expiredAt = ConvertExpiredDateToUtc(request.ExpiredDate);
        var nowUtc = DateTimeOffset.UtcNow;

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

            if (request.Skills is null || request.Skills.Count == 0)
            {
                requiredFields.Add("skills");
            }

            if (string.IsNullOrWhiteSpace(request.ShortDescription))
            {
                requiredFields.Add("shortDescription");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                requiredFields.Add("description");
            }

            if (string.IsNullOrWhiteSpace(request.Requirements))
            {
                requiredFields.Add("requirements");
            }

            if (requiredFields.Count > 0)
            {
                throw new JobPostException(
                    "JOB_POST_VALIDATION_FAILED",
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

        var skills = request.Skills?
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .ToList() ?? new List<string>();

        if (isPublishing && skills.Count == 0)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_FAILED",
                "Danh sách kỹ năng phải có ít nhất một giá trị.",
                "skills");
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
            ShortDescription = request.ShortDescription?.Trim(),
            Description = request.Description?.Trim(),
            Requirements = request.Requirements?.Trim()
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

        // 3. Parse status filter từ string sang enum C#.
        JobPostStatus? statusFilter = null;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var isValidStatus = Enum.TryParse<JobPostStatus>(
                request.Status,
                ignoreCase: false,
                out var parsedStatus);

            if (!isValidStatus || !Enum.IsDefined(parsedStatus))
            {
                throw new JobPostException(
                    "JOB_POST_INVALID_STATUS_FILTER",
                    "Trạng thái lọc không hợp lệ.",
                    "status");
            }

            statusFilter = parsedStatus;
        }

        // 4. Bắt đầu query danh sách JobPost.
        var jobPostsQuery = _dbContext.JobPosts.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();

            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                jobPost.Title.ToLower().Contains(searchLower));
        }

        if (statusFilter.HasValue)
        {
            jobPostsQuery = jobPostsQuery.Where(jobPost =>
                jobPost.Status == statusFilter.Value);
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
                PendingApplicationCount = jobPost.PendingApplicationCount
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
            Department = jobPost.Department?.Name,
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
            CanEdit = canEdit
        };
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

        var nowUtc = DateTimeOffset.UtcNow;
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

        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300)
        {
            throw new JobPostException(
                "JOB_POST_VALIDATION_ERROR",
                "Tiêu đề tin tuyển dụng không hợp lệ.",
                "title");
        }

        var department = await FindDepartmentAsync(request.DepartmentId);
        var skills = NormalizeSkills(request.Skills);

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

            if (skills.Count == 0)
            {
                requiredFields.Add("skills");
            }

            if (string.IsNullOrWhiteSpace(request.ShortDescription))
            {
                requiredFields.Add("shortDescription");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                requiredFields.Add("description");
            }

            if (string.IsNullOrWhiteSpace(request.Requirements))
            {
                requiredFields.Add("requirements");
            }

            if (!expiredAt.HasValue || expiredAt.Value <= nowUtc)
            {
                requiredFields.Add("expiredDate");
            }

            if (requiredFields.Count > 0)
            {
                throw new JobPostException(
                    "JOB_POST_VALIDATION_ERROR",
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
        jobPost.ShortDescription = request.ShortDescription?.Trim();
        jobPost.Description = request.Description?.Trim();
        jobPost.Requirements = request.Requirements?.Trim();
        jobPost.ExpiredAt = expiredAt;
        jobPost.Status = request.Action == JobPostAction.Publish
            ? JobPostStatus.Open
            : JobPostStatus.Draft;
        jobPost.UpdatedAt = nowUtc;

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

    private static List<string> NormalizeSkills(List<string>? skills)
    {
        if (skills is null)
        {
            return new List<string>();
        }

        return skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .ToList();
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
            Department = jobPost.Department?.Name,
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
            Requirements = jobPost.Requirements
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
