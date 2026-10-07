using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity.Enum;

namespace VNZ.Api.BackgroundJob;

public class JobPostExpirationBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _serviceScopeFactory;

    public JobPostExpirationBackgroundService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nowUtc = VNZ.Service.Utils.DateTimeOffsetPrecision.UtcNowMicrosecond();

            var expiredJobPosts = await dbContext.JobPosts
                .Where(jobPost =>
                    jobPost.Status == JobPostStatus.Open &&
                    jobPost.ExpiredAt.HasValue &&
                    jobPost.ExpiredAt.Value <= nowUtc)
                .ToListAsync(stoppingToken);

            foreach (var jobPost in expiredJobPosts)
            {
                jobPost.Status = JobPostStatus.Expired;
                jobPost.UpdatedAt = nowUtc;
            }

            if (expiredJobPosts.Count > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }
}
