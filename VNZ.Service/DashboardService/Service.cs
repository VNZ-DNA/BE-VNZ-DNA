using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;

namespace VNZ.Service.DashboardService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Response.DashboardResponse> GetDashboardAsync()
    {
        var nowUtc = DateTimeOffset.UtcNow;

        await _dbContext.JobPosts
            .Where(jobPost =>
                jobPost.Status == JobPostStatus.Open &&
                jobPost.ExpiredAt.HasValue &&
                jobPost.ExpiredAt.Value <= nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(jobPost => jobPost.Status, JobPostStatus.Expired)
                .SetProperty(jobPost => jobPost.UpdatedAt, nowUtc));

        var totalNewsCount = await _dbContext.NewsArticles
            .AsNoTracking()
            .CountAsync();

        var newsByStatusRows = await _dbContext.NewsArticles
            .AsNoTracking()
            .GroupBy(article => article.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var openJobsCount = await _dbContext.JobPosts
            .AsNoTracking()
            .CountAsync(jobPost =>
                jobPost.Status == JobPostStatus.Open &&
                jobPost.ExpiredAt.HasValue &&
                jobPost.ExpiredAt.Value > nowUtc);

        var pendingApplicationsCount = await _dbContext.JobApplications
            .AsNoTracking()
            .CountAsync(application => application.Status == JobApplicationStatus.Pending);

        var unreadContactsCount = await _dbContext.ContactInquiries
            .AsNoTracking()
            .CountAsync(contact => !contact.IsRead);

        var recentPostRows = await _dbContext.NewsArticles
            .AsNoTracking()
            .Where(article => article.Creator != null)
            .OrderByDescending(article => article.CreatedAt)
            .Take(5)
            .Select(article => new
            {
                Id = article.Id,
                Title = article.Title,
                AuthorName = article.Creator!.FullName,
                Status = article.Status,
                CreatedAtUtc = article.CreatedAt
            })
            .ToListAsync();

        var recentPosts = recentPostRows
            .Select(article => new Response.RecentPostResponse
            {
                Id = article.Id,
                Title = article.Title,
                AuthorName = article.AuthorName,
                Status = GetDisplayName(article.Status),
                CreatedAtUtc = article.CreatedAtUtc
            })
            .ToList();

        var pendingTasks = new List<Response.PendingTaskResponse>();

        if (pendingApplicationsCount > 0)
        {
            pendingTasks.Add(new Response.PendingTaskResponse
            {
                Type = "PendingApplications",
                Count = pendingApplicationsCount,
                Label = "Hồ sơ ứng tuyển chờ duyệt"
            });
        }

        if (unreadContactsCount > 0)
        {
            pendingTasks.Add(new Response.PendingTaskResponse
            {
                Type = "UnreadContacts",
                Count = unreadContactsCount,
                Label = "Yêu cầu liên hệ chưa đọc"
            });
        }

        return new Response.DashboardResponse
        {
            TotalNewsCount = totalNewsCount,
            NewsByStatus = newsByStatusRows.ToDictionary(
                row => GetDisplayName(row.Status),
                row => row.Count),
            OpenJobsCount = openJobsCount,
            PendingApplicationsCount = pendingApplicationsCount,
            UnreadContactsCount = unreadContactsCount,
            PendingTasks = pendingTasks,
            RecentPosts = recentPosts
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
