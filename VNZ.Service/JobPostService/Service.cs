using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.Exceptions;

namespace VNZ.Service.JobPostService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
        var items = new List<Response.JobPostListItemResponse>();

        foreach (var jobPost in jobPosts)
        {
            items.Add(new Response.JobPostListItemResponse
            {
                Id = jobPost.Id,
                Title = jobPost.Title,
                ShortDescription = jobPost.ShortDescription,
                ExpiredAt = jobPost.ExpiredAt,
                NumberOfPositions = jobPost.NumberOfPositions,
                Status = GetDisplayName(jobPost.Status),
                PendingApplicationCount = jobPost.PendingApplicationCount
            });
        }

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
            ExpiredAt = jobPost.ExpiredAt,
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
