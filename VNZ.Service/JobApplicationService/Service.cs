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

    public async Task<Response.JobApplicationListResponse> GetJobApplicationListAsync(Request.GetJobApplicationListRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

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
            .Include(application => application.JobPost)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(application => application.Status == statusFilter.Value);
        }

        var applications = await query
            .OrderByDescending(application => application.CreatedAt)
            .ThenByDescending(application => application.Id)
            .Select(application => new Response.JobApplicationListItemResponse
            {
                Id = application.Id,
                FullName = application.FullName,
                Email = application.Email,
                JobPostId = application.JobPostId,
                JobPostTitle = application.JobPost.Title,
                Status = application.Status.ToString(),
                CreatedAt = application.CreatedAt,
                InterviewAt = application.InterViewAt
            })
            .ToListAsync();

        return new Response.JobApplicationListResponse
        {
            Items = applications,
            Total = applications.Count
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
