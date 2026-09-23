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
