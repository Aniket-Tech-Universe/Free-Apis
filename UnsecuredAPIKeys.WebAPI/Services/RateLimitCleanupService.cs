using Microsoft.EntityFrameworkCore;
using UnsecuredAPIKeys.Data;

namespace UnsecuredAPIKeys.WebAPI.Services
{
    public interface IRateLimitCleanupService
    {
        Task CleanupOldRecordsAsync();
    }

    public class RateLimitCleanupService(IServiceProvider serviceProvider, ILogger<RateLimitCleanupService> logger)
        : IRateLimitCleanupService
    {
        public async Task CleanupOldRecordsAsync()
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

                // Delete records older than 7 days (well beyond any reasonable rate limit window)
                var cutoffTime = DateTime.UtcNow.AddDays(-7);
                
                var deletedCount = await dbContext.RateLimitLogs
                    .Where(r => r.RequestTimeUtc < cutoffTime)
                    .ExecuteDeleteAsync();

                if (deletedCount > 0)
                {
                    logger.LogInformation("Cleaned up {DeletedCount} old rate limit records older than {CutoffTime}", 
                        deletedCount, cutoffTime);
                }
                else
                {
                    logger.LogDebug("No old rate limit records found to clean up");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during rate limit cleanup");
            }
        }
    }

    public class RateLimitCleanupHostedService(
        IServiceProvider serviceProvider,
        ILogger<RateLimitCleanupHostedService> logger)
        : BackgroundService
    {
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Run cleanup every 6 hours

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Rate limit cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var cleanupService = scope.ServiceProvider.GetRequiredService<IRateLimitCleanupService>();
                    await cleanupService.CleanupOldRecordsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in rate limit cleanup background service");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            logger.LogInformation("Rate limit cleanup service stopped");
        }
    }
}
