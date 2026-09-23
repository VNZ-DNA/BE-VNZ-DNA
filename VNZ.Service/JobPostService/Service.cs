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

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        // 4. Đổi tất cả JobPost Open đã quá hạn thành Expired.
        var nowUtc = DateTimeOffset.UtcNow;
        var expiredJobPosts = await _dbContext.JobPosts
            .Where(jobPost =>
                jobPost.Status == JobPostStatus.Open &&
                jobPost.ExpiredAt.HasValue &&
                jobPost.ExpiredAt.Value <= nowUtc)
            .ToListAsync();

        foreach (var jobPost in expiredJobPosts)
        {
            jobPost.Status = JobPostStatus.Expired;
            jobPost.UpdatedAt = nowUtc;
        }

        if (expiredJobPosts.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        // 5. Bắt đầu query danh sách JobPost.
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

        // 6. Đếm tổng sau filter, sau đó lấy đúng trang cần xem.
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

        // 7. Map dữ liệu database sang response API.
        var items = new List<Response.JobPostListItemResponse>();

        foreach (var jobPost in jobPosts)
        {
            string statusLabel;

            if (jobPost.Status == JobPostStatus.Draft)
            {
                statusLabel = "Bản nháp";
            }
            else if (jobPost.Status == JobPostStatus.Open)
            {
                statusLabel = "Đang tuyển";
            }
            else if (jobPost.Status == JobPostStatus.Closed)
            {
                statusLabel = "Đã đóng";
            }
            else if (jobPost.Status == JobPostStatus.Expired)
            {
                statusLabel = "Đã hết hạn";
            }
            else
            {
                throw new ArgumentOutOfRangeException();
            }

            items.Add(new Response.JobPostListItemResponse
            {
                Id = jobPost.Id,
                Title = jobPost.Title,
                ShortDescription = jobPost.ShortDescription,
                ExpiredAt = jobPost.ExpiredAt,
                NumberOfPositions = jobPost.NumberOfPositions,
                Status = statusLabel,
                PendingApplicationCount = jobPost.PendingApplicationCount
            });
        }

        await transaction.CommitAsync();

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
}
