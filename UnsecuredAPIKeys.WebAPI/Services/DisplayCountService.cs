using Microsoft.EntityFrameworkCore;
using UnsecuredAPIKeys.Data;

namespace UnsecuredAPIKeys.WebAPI.Services
{
    public interface IDisplayCountService
    {
        long TotalDisplayCount { get; }
        Task InitializeAsync();
        void IncrementCount();
    }

    public class DisplayCountService(IServiceScopeFactory scopeFactory, ILogger<DisplayCountService> logger)
        : IDisplayCountService
    {
        private long _totalDisplayCount;

        public long TotalDisplayCount => _totalDisplayCount;

        public async Task InitializeAsync()
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
            
            try
            {
                // Calculate the total display count from all API keys
                _totalDisplayCount = await dbContext.APIKeys
                    .SumAsync(k => (long)k.TimesDisplayed);
                
                logger.LogInformation("Initialized display count service with total: {TotalCount}", _totalDisplayCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize display count");
                _totalDisplayCount = 0;
            }
        }

        public void IncrementCount()
        {
            Interlocked.Increment(ref _totalDisplayCount);
            logger.LogDebug("Display count incremented to: {TotalCount}", _totalDisplayCount);
        }
    }
}
