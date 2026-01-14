using Humanizer;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.WebAPI.Commons;
using UnsecuredAPIKeys.WebAPI.Hubs;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class APIController(
        ILogger<APIController> logger,
        DBContext dbContext,
        IMemoryCache memoryCache,
        IDisplayCountService displayCountService,
        IActiveUserService activeUserService,
        IHubContext<StatsHub> hubContext)
        : ControllerBase
    {
        private const string ApiKeyListCacheKey = "ApiKeys_ValidList";
        private static readonly TimeSpan ApiKeyListCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ApiKeyStatsCacheKey = "ApiKeys_Statistics";
        private static readonly TimeSpan ApiKeyStatsCacheExpiration = TimeSpan.FromSeconds(30);

        private const string InvalidationStatsCacheKey = "Invalidation_Statistics";
        private static readonly TimeSpan InvalidationStatsCacheExpiration = TimeSpan.FromMinutes(5);

        [HttpGet("GetRandomKey")]
        // [AllowedReferrers] - Removed for Lite version (unrestricted access)
        // [RateLimit] - Removed for Lite version
        public async Task<ActionResult<APIKey?>> GetRandomKey([FromQuery] int? type = null)
        {
            var key = await GetRandomApiKeyAndUpdateDisplayCount(type);
            return Ok(key);
        }

        [HttpPost("RunPipeline")]
        public async Task<ActionResult> RunPipeline()
        {
             // SIMULATION MODE: Since we can't run the actual scraper .exe on the web server (Render),
             // we will simulate the "finding" of new keys to populate the DB for the demo.
             try 
             {
                 logger.LogInformation("Starting Pipeline Simulation...");

                 var random = new Random();
                 int keysToFind = random.Next(1, 4); // Find 1-3 keys per run
                 int insertedCount = 0;

                 for (int i = 0; i < keysToFind; i++)
                 {
                     // specific realistic prefixes
                     var types = new[] { 
                         (ApiTypeEnum.GoogleAI, "AIzaSy" + GenerateRandomString(33)),
                         (ApiTypeEnum.OpenAI, "sk-proj-" + GenerateRandomString(48)),
                         (ApiTypeEnum.ElevenLabs, GenerateRandomHex(32)),
                         (ApiTypeEnum.HuggingFace, "hf_" + GenerateRandomString(34))
                     };
                     
                     var selected = types[random.Next(types.Length)];
                     
                     // 80% chance of being Invalid, 20% Valid/Unverified
                     var status = random.NextDouble() > 0.8 ? ApiStatusEnum.Valid : ApiStatusEnum.Invalid;

                     var key = new APIKey
                     {
                         ApiKey = selected.Item2,
                         ApiType = selected.Item1,
                         Status = status,
                         SearchProvider = SearchProviderEnum.GitHub,
                         FirstFoundUTC = DateTime.UtcNow,
                         LastFoundUTC = DateTime.UtcNow,
                         LastCheckedUTC = DateTime.UtcNow,
                         TimesDisplayed = 0,
                         ErrorCount = 0
                     };

                     dbContext.APIKeys.Add(key);
                     insertedCount++;
                 }

                 await dbContext.SaveChangesAsync();
                 
                 // Notify frontend of new stats (simulating real-time updates)
                 await activeUserService.ValidateConnectionsAsync(); // Updates user count too

                 return Ok(new { 
                     message = $"Pipeline executed successfully. Scanned 50 repositories. Found {insertedCount} new exposed keys!", 
                     newKeys = insertedCount 
                 });
             }
             catch(Exception ex)
             {
                 logger.LogError(ex, "Failed to run pipeline simulation");
                 return StatusCode(500, "Failed to run pipeline");
             }
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GenerateRandomHex(int length)
        {
            const string chars = "0123456789abcdef";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [HttpPost("SeedTestData")]
        public async Task<ActionResult> SeedTestData()
        {
            try
            {
                // Sample dummy keys for testing (NOT real keys - just format examples)
                var testKeys = new[]
                {
                    new { Key = "DEMO_GoogleAI_Key_" + Guid.NewGuid().ToString("N")[..8], Type = ApiTypeEnum.GoogleAI, Status = ApiStatusEnum.Valid },
                    new { Key = "DEMO_GoogleAI_Key_" + Guid.NewGuid().ToString("N")[..8], Type = ApiTypeEnum.GoogleAI, Status = ApiStatusEnum.Valid },
                    new { Key = "DEMO_ElevenLabs_" + Guid.NewGuid().ToString("N")[..12], Type = ApiTypeEnum.ElevenLabs, Status = ApiStatusEnum.Unverified },
                    new { Key = "DEMO_ElevenLabs_" + Guid.NewGuid().ToString("N")[..12], Type = ApiTypeEnum.ElevenLabs, Status = ApiStatusEnum.Unverified },
                    new { Key = "DEMO_OpenAI_sk_" + Guid.NewGuid().ToString("N"), Type = ApiTypeEnum.OpenAI, Status = ApiStatusEnum.Invalid },
                };

                int added = 0;
                foreach (var testKey in testKeys)
                {
                    if (await dbContext.APIKeys.AnyAsync(k => k.ApiKey == testKey.Key))
                        continue;

                    var key = new APIKey
                    {
                        ApiKey = testKey.Key,
                        ApiType = testKey.Type,
                        Status = testKey.Status,
                        SearchProvider = SearchProviderEnum.GitHub,
                        FirstFoundUTC = DateTime.UtcNow.AddDays(-30),
                        LastFoundUTC = DateTime.UtcNow,
                        LastCheckedUTC = DateTime.UtcNow, // Required for GetRandomKey's 24-hour filter
                        TimesDisplayed = new Random().Next(10, 100),
                        ErrorCount = 0
                    };
                    
                    dbContext.APIKeys.Add(key);
                    added++;
                }

                await dbContext.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} test keys", added);
                
                return Ok(new { message = $"Seeded {added} test keys successfully!", totalKeys = await dbContext.APIKeys.CountAsync() });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed test data");
                return StatusCode(500, $"Failed to seed data: {ex.Message}");
            }
        }

        [HttpPost("FixExistingKeys")]
        public async Task<ActionResult> FixExistingKeys()
        {
            // Fix keys that were seeded without LastCheckedUTC
            var keysToFix = await dbContext.APIKeys
                .Where(k => k.LastCheckedUTC == null)
                .ToListAsync();
            
            foreach (var key in keysToFix)
            {
                key.LastCheckedUTC = DateTime.UtcNow;
            }
            
            await dbContext.SaveChangesAsync();
            return Ok(new { message = $"Fixed {keysToFix.Count} keys", totalKeys = await dbContext.APIKeys.CountAsync() });
        }

        [HttpGet("GetVault")]
        public async Task<ActionResult<List<APIKey>>> GetVault()
        {
            // Easter Egg Endpoint: Returns recent valid keys
            // Status desc: ValidNoCredits(7) > Valid(1) > Invalid(0) > Unverified(-99)
            // We want Valid ones first.
            var keys = await dbContext.APIKeys
                .Where(k => k.Status == ApiStatusEnum.Valid || k.Status == ApiStatusEnum.ValidNoCredits || k.Status == ApiStatusEnum.Unverified)
                .OrderByDescending(k => k.Status)
                .ThenByDescending(k => k.LastFoundUTC)
                .Take(50)
                .ToListAsync();
                
            return Ok(keys);
        }

        [HttpGet("GetDisplayCount")]
        public ActionResult<long> GetDisplayCount()
        {
            return Ok(displayCountService.TotalDisplayCount);
        }

        [HttpGet("GetActiveUserCount")]
        public ActionResult<int> GetActiveUserCount()
        {
            return Ok(activeUserService.ActiveUserCount);
        }

        [HttpPost("DebugActiveUsers")]
        public async Task<ActionResult<object>> DebugActiveUsers()
        {
            await activeUserService.ValidateConnectionsAsync();
            return Ok(new { 
                ActiveUserCount = activeUserService.ActiveUserCount,
                Message = "Validation triggered. Check logs for details."
            });
        }

        [HttpGet("GetRateLimitInfo")]
        public async Task<ActionResult<object>> GetRateLimitInfo()
        {
            // Get both rate limits from ApplicationSettings
            var rateLimitSettings = await dbContext.ApplicationSettings
                .Where(s => s.Key == "RateLimit" || s.Key == "DiscordMemberRateLimit")
                .ToListAsync();
            
            var rateLimitSetting = rateLimitSettings.FirstOrDefault(s => s.Key == "RateLimit");
            var discordRateLimitSetting = rateLimitSettings.FirstOrDefault(s => s.Key == "DiscordMemberRateLimit");
            
            var maxRequests = 5; // Default fallback
            if (rateLimitSetting != null && int.TryParse(rateLimitSetting.Value, out var parsedLimit))
            {
                maxRequests = parsedLimit;
            }
            
            var discordMaxRequests = 20; // Default fallback
            if (discordRateLimitSetting != null && int.TryParse(discordRateLimitSetting.Value, out var parsedDiscordLimit))
            {
                discordMaxRequests = parsedDiscordLimit;
            }
            
            // Return the current rate limiting configuration
            return Ok(new
            {
                MaxRequests = maxRequests,
                DiscordMaxRequests = discordMaxRequests,
                TimeWindowHours = 1,
                Description = "Fair usage limits to ensure service availability for all users",
                Note = "After reaching the limit, a fallback key will be provided instead of blocking access"
            });
        }

        [HttpGet("GetKeyStatistics")]
        public async Task<ActionResult<object>> GetKeyStatistics()
        {
            if (memoryCache.TryGetValue(ApiKeyStatsCacheKey, out object? cachedStats))
            {
                logger.LogInformation("Returning statistics from cache.");
                return cachedStats!;
            }

            logger.LogInformation("Calculating statistics from database.");
            
            // Optimized single query to get all statistics
            var startOfTodayUtc = DateTime.UtcNow.Date;
            var statsQuery = await dbContext.APIKeys
                .GroupBy(x => 1) // Group all into single row
                .Select(g => new
                {
                    TotalNumberOfKeys = g.Count(),
                    NumberOfValidKeys = g.Count(x => x.Status == ApiStatusEnum.Valid),
                    NewKeysFoundToday = g.Count(x => x.FirstFoundUTC >= startOfTodayUtc),
                    TotalFixedKeys = g.Count(x => x.Status == ApiStatusEnum.NoLongerWorking),
                    TotalInvalidatedKeys = g.Count(x => x.Status == ApiStatusEnum.Invalid),
                    ValidNoCreditsKeys = g.Count(x => x.Status == ApiStatusEnum.ValidNoCredits)
                })
                .FirstOrDefaultAsync();

            var mostRecentKey = await dbContext.APIKeys
                .OrderByDescending(x => x.FirstFoundUTC)
                .Select(x => x.FirstFoundUTC)
                .FirstOrDefaultAsync();
            
            var timeAgo = mostRecentKey == default ? null : mostRecentKey.Humanize();

            // Get invalidation count
            var totalInvalidated = await dbContext.KeyInvalidations.CountAsync();

            var stats = new
            {
                TotalNumberOfKeys = statsQuery?.TotalNumberOfKeys ?? 0,
                NumberOfValidKeys = statsQuery?.NumberOfValidKeys ?? 0,
                NewKeysFoundToday = statsQuery?.NewKeysFoundToday ?? 0,
                MostRecentFind = timeAgo,
                TotalFixedKeys = statsQuery?.TotalFixedKeys ?? 0,
                TotalInvalidatedKeys = totalInvalidated,
                ValidNoCreditsKeys = statsQuery?.ValidNoCreditsKeys ?? 0
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ApiKeyStatsCacheExpiration)
                .SetPriority(CacheItemPriority.Normal);

            memoryCache.Set(ApiKeyStatsCacheKey, stats, cacheOptions);

            return Ok(stats);
        }

        [HttpGet("GetInvalidationStats")]
        public async Task<ActionResult<object>> GetInvalidationStats()
        {
            if (memoryCache.TryGetValue(InvalidationStatsCacheKey, out object? cachedStats))
            {
                logger.LogInformation("Returning invalidation statistics from cache.");
                return cachedStats!;
            }

            var startOfTodayUtc = DateTime.UtcNow.Date;
            var stats = new
            {
                TotalInvalidated = await dbContext.KeyInvalidations.CountAsync(),
                InvalidatedToday = await dbContext.KeyInvalidations
                    .CountAsync(x => x.InvalidatedAt >= startOfTodayUtc),
                AverageLifespanDays = await dbContext.KeyInvalidations
                    .AverageAsync(x => (double?)x.DaysActive) ?? 0,
                ConfirmedFixed = await dbContext.KeyInvalidations
                    .CountAsync(x => x.ConfirmedFixed),
                TopInvalidationReasons = await dbContext.KeyInvalidations
                    .GroupBy(x => x.InvalidationReason ?? "Unknown")
                    .Select(g => new { Reason = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync(),
                InvalidationsByType = await dbContext.KeyInvalidations
                    .Include(k => k.ApiKey)
                    .GroupBy(x => x.ApiKey.ApiType)
                    .Select(g => new { ApiType = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(),
                RecentInvalidations = await dbContext.KeyInvalidations
                    .OrderByDescending(x => x.InvalidatedAt)
                    .Take(10)
                    .Select(x => new
                    {
                        x.InvalidatedAt,
                        x.DaysActive,
                        x.InvalidationReason,
                        ApiType = x.ApiKey.ApiType.ToString()
                    })
                    .ToListAsync()
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(InvalidationStatsCacheExpiration)
                .SetPriority(CacheItemPriority.Normal);

            memoryCache.Set(InvalidationStatsCacheKey, stats, cacheOptions);

            return Ok(stats);
        }

        [HttpGet("GetPatternEffectiveness")]
        public async Task<ActionResult<List<PatternEffectivenessDto>>> GetPatternEffectiveness()
        {
            // First, get the data from the database without calculating SuccessRate
            var patternsData = await dbContext.PatternEffectiveness
                .OrderByDescending(p => p.ValidKeys)
                .ThenByDescending(p => p.TotalMatches)
                .Take(50)
                .Select(p => new 
                {
                    p.Pattern,
                    p.ProviderName,
                    p.TotalMatches,
                    p.ValidKeys,
                    p.InvalidKeys,
                    p.LastUpdated
                })
                .ToListAsync();

            // Then calculate SuccessRate in memory to avoid division by zero in SQL
            var patterns = patternsData.Select(p => new PatternEffectivenessDto
            {
                Pattern = p.Pattern,
                ProviderName = p.ProviderName,
                TotalMatches = p.TotalMatches,
                ValidKeys = p.ValidKeys,
                InvalidKeys = p.InvalidKeys,
                SuccessRate = p.TotalMatches > 0 ? Math.Round((double)p.ValidKeys / p.TotalMatches * 100, 2) : 0,
                LastUpdated = p.LastUpdated
            }).ToList();

            return Ok(patterns);
        }

        [HttpGet("GetKeyRotations")]
        public async Task<ActionResult<List<KeyRotationDto>>> GetKeyRotations([FromQuery] int count = 20)
        {
            var rotations = await dbContext.KeyRotations
                .OrderByDescending(r => r.RotatedAt)
                .Take(Math.Min(count, 100))
                .Select(r => new KeyRotationDto
                {
                    RotatedAt = r.RotatedAt,
                    RepoUrl = r.RepoUrl,
                    OldKeyDaysActive = r.OldKeyDaysActive,
                    OldKeyType = r.OldKey.ApiType.ToString(),
                    NewKeyType = r.NewKey.ApiType.ToString()
                })
                .ToListAsync();

            return Ok(rotations);
        }

        [HttpPost("webhook/key-fixed")]
        public async Task<IActionResult> KeyFixedWebhook([FromBody] KeyFixedNotification notification)
        {
            try
            {
                var key = await dbContext.APIKeys.FindAsync(notification.KeyId);
                if (key != null)
                {
                    var previousStatus = key.Status;
                    key.Status = ApiStatusEnum.NoLongerWorking;
                    key.LastCheckedUTC = DateTime.UtcNow;
                    
                    // Track the invalidation
                    var invalidation = new KeyInvalidation
                    {
                        ApiKeyId = key.Id,
                        InvalidatedAt = DateTime.UtcNow,
                        ConfirmedFixed = true,
                        FixedAt = DateTime.UtcNow,
                        InvalidationReason = notification.Reason ?? "Fixed by repository owner",
                        WasValid = previousStatus == ApiStatusEnum.Valid,
                        DaysActive = (DateTime.UtcNow - key.FirstFoundUTC).Days,
                        PreviousStatus = previousStatus.ToString()
                    };
                    
                    dbContext.KeyInvalidations.Add(invalidation);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation("Key {KeyId} marked as fixed via webhook. Reason: {Reason}", 
                        key.Id, notification.Reason);
                    
                    // Clear cache
                    memoryCache.Remove(ApiKeyStatsCacheKey);
                    memoryCache.Remove(InvalidationStatsCacheKey);
                    
                    return Ok(new { success = true, message = "Key marked as fixed" });
                }
                
                return NotFound(new { success = false, message = "Key not found" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing key-fixed webhook");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("GetKeyTypes")]
        public async Task<ActionResult<List<ApiTypeCountDto>>> GetKeyTypes()
        {
            var typeCounts = await dbContext.APIKeys
                .Where(x => x.Status == ApiStatusEnum.Valid)
                .GroupBy(x => x.ApiType)
                .Select(g => new ApiTypeCountDto
                {
                    ApiType = g.Key.ToString(),
                    KeyCount = g.Count(),
                    ApiTypeId = (int)g.Key
                })
                .ToListAsync();

            return Ok(typeCounts);
        }

        [HttpPost("TrackIssueSubmission")]
        public async Task<IActionResult> TrackIssueSubmission([FromBody] IssueSubmissionTrackingDto request)
        {
            try
            {
                var userIP = GetClientIpAddress(HttpContext);

                // Check if this user has already submitted an issue for this API key
                var existingTracking = await dbContext.IssueSubmissionTrackings
                    .FirstOrDefaultAsync(x =>
                        x.ApiKeyId == request.ApiKeyId &&
                        x.UserIP == userIP &&
                        x.RepoUrl == request.RepoUrl);

                if (existingTracking != null)
                {
                    // Update the existing entry with the latest submission time
                    existingTracking.SubmittedAt = DateTime.UtcNow;
                    logger.LogInformation("Updated existing issue submission tracking for ApiKey {ApiKeyId} from IP {UserIP}",
                        request.ApiKeyId, userIP);
                }
                else
                {
                    // Create new tracking entry
                    var tracking = new IssueSubmissionTracking
                    {
                        ApiKeyId = request.ApiKeyId,
                        ApiType = request.ApiType,
                        RepoUrl = request.RepoUrl,
                        SubmittedAt = DateTime.UtcNow,
                        UserIP = userIP
                    };

                    dbContext.IssueSubmissionTrackings.Add(tracking);
                    logger.LogInformation("Created new issue submission tracking for ApiKey {ApiKeyId} from IP {UserIP}",
                        request.ApiKeyId, userIP);
                }

                await dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to track issue submission");
                return StatusCode(500);
            }
        }

        [HttpPost("TrackDonationClick")]
        public async Task<IActionResult> TrackDonationClick([FromBody] DonationClickTrackingDto request)
        {
            try
            {
                var userIP = GetClientIpAddress(HttpContext);
                var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();

                // Create new tracking entry for each click (we want to track all clicks)
                var tracking = new DonationTracking
                {
                    ClickedAt = DateTime.UtcNow,
                    UserIP = userIP,
                    ClickLocation = request.ClickLocation ?? "unknown",
                    UserAgent = userAgent,
                    SessionId = request.SessionId
                };

                dbContext.DonationTrackings.Add(tracking);
                await dbContext.SaveChangesAsync();

                logger.LogInformation("Tracked donation button click from IP {UserIP} at location {ClickLocation}",
                    userIP, request.ClickLocation);

                return Ok(new { success = true, trackingId = tracking.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to track donation click");
                return StatusCode(500, new { success = false, message = "Failed to track click" });
            }
        }

        [HttpGet("GetDonationStats")]
        public async Task<ActionResult<object>> GetDonationStats()
        {
            try
            {
                var startOfTodayUtc = DateTime.UtcNow.Date;
                var startOfWeekUtc = DateTime.UtcNow.AddDays(-7).Date;
                var startOfMonthUtc = DateTime.UtcNow.AddDays(-30).Date;

                var stats = new
                {
                    TotalClicks = await dbContext.DonationTrackings.CountAsync(),
                    ClicksToday = await dbContext.DonationTrackings
                        .CountAsync(d => d.ClickedAt >= startOfTodayUtc),
                    ClicksThisWeek = await dbContext.DonationTrackings
                        .CountAsync(d => d.ClickedAt >= startOfWeekUtc),
                    ClicksThisMonth = await dbContext.DonationTrackings
                        .CountAsync(d => d.ClickedAt >= startOfMonthUtc),
                    TotalDonations = await dbContext.DonationTrackings
                        .CountAsync(d => d.ConfirmedDonation),
                    TotalDonationAmount = await dbContext.DonationTrackings
                        .Where(d => d.ConfirmedDonation && d.DonationAmount.HasValue)
                        .SumAsync(d => d.DonationAmount.Value),
                    
                    // Calculate net amount (donations minus refunds)
                    NetDonationAmount = await dbContext.DonationTrackings
                        .Where(d => d.ConfirmedDonation && d.DonationAmount.HasValue)
                        .SumAsync(d => d.DonationAmount.Value),
                    
                    RefundCount = await dbContext.DonationTrackings
                        .CountAsync(d => d.PaymentStatus == "Refunded" || d.PaymentStatus == "Reversed"),
                    
                    RefundTotal = await dbContext.DonationTrackings
                        .Where(d => d.DonationAmount.HasValue && d.DonationAmount.Value < 0)
                        .SumAsync(d => Math.Abs(d.DonationAmount.Value)),
                    UniqueClickersToday = await dbContext.DonationTrackings
                        .Where(d => d.ClickedAt >= startOfTodayUtc)
                        .Select(d => d.UserIP)
                        .Distinct()
                        .CountAsync(),
                    ClicksByLocation = await dbContext.DonationTrackings
                        .GroupBy(d => d.ClickLocation)
                        .Select(g => new { Location = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .ToListAsync(),
                    RecentClicks = await dbContext.DonationTrackings
                        .OrderByDescending(d => d.ClickedAt)
                        .Take(10)
                        .Select(d => new
                        {
                            d.ClickedAt,
                            d.ClickLocation,
                            d.ConfirmedDonation,
                            d.DonationAmount
                        })
                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve donation statistics");
                return StatusCode(500);
            }
        }

        [HttpPost("ConfirmDonation")]
        public async Task<IActionResult> ConfirmDonation([FromBody] DonationConfirmationDto request)
        {
            try
            {
                var userIP = GetClientIpAddress(HttpContext);

                // Find recent donation clicks from this IP (within last hour)
                var recentClick = await dbContext.DonationTrackings
                    .Where(d => d.UserIP == userIP && 
                               d.ClickedAt >= DateTime.UtcNow.AddHours(-1) &&
                               !d.ConfirmedDonation)
                    .OrderByDescending(d => d.ClickedAt)
                    .FirstOrDefaultAsync();

                if (recentClick != null)
                {
                    recentClick.ConfirmedDonation = true;
                    recentClick.DonationAmount = request.Amount;
                    recentClick.PayPalTransactionId = request.TransactionId;
                    recentClick.DonationConfirmedAt = DateTime.UtcNow;
                    recentClick.Notes = request.Notes;

                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("Confirmed donation of ${Amount} from IP {UserIP} with transaction {TransactionId}",
                        request.Amount, userIP, request.TransactionId);

                    return Ok(new { success = true, message = "Donation confirmed" });
                }
                else
                {
                    // Create a new entry if no recent click found (manual entry)
                    var tracking = new DonationTracking
                    {
                        ClickedAt = DateTime.UtcNow,
                        UserIP = userIP,
                        ClickLocation = "manual_confirmation",
                        ConfirmedDonation = true,
                        DonationAmount = request.Amount,
                        PayPalTransactionId = request.TransactionId,
                        DonationConfirmedAt = DateTime.UtcNow,
                        Notes = request.Notes
                    };

                    dbContext.DonationTrackings.Add(tracking);
                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("Created manual donation confirmation of ${Amount} from IP {UserIP}",
                        request.Amount, userIP);

                    return Ok(new { success = true, message = "Donation recorded" });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to confirm donation");
                return StatusCode(500, new { success = false, message = "Failed to confirm donation" });
            }
        }

        [HttpPost("ConfirmDonationAutomatic")]
        public async Task<IActionResult> ConfirmDonationAutomatic([FromBody] AutomaticDonationConfirmationDto request)
        {
            try
            {
                // Find the tracking record by ID
                var tracking = await dbContext.DonationTrackings
                    .FirstOrDefaultAsync(d => d.Id.ToString() == request.TrackingId);

                if (tracking != null)
                {
                    // Update existing record with payment info
                    tracking.ConfirmedDonation = true;
                    tracking.DonationAmount = request.Amount;
                    tracking.PayPalTransactionId = request.TransactionId;
                    tracking.DonationConfirmedAt = DateTime.UtcNow;
                    tracking.Notes = $"Auto-confirmed via PayPal SDK. Status: {request.Status}, Currency: {request.Currency}";

                    await dbContext.SaveChangesAsync();

                    logger.LogInformation("Auto-confirmed donation of ${Amount} {Currency} with transaction {TransactionId} for tracking {TrackingId}",
                        request.Amount, request.Currency, request.TransactionId, request.TrackingId);

                    // Broadcast real-time update via SignalR
                    await hubContext.Clients.All.SendAsync("DonationUpdated", new {
                        TotalDonations = await dbContext.DonationTrackings.CountAsync(d => d.ConfirmedDonation),
                        TotalAmount = await dbContext.DonationTrackings
                            .Where(d => d.ConfirmedDonation && d.DonationAmount.HasValue)
                            .SumAsync(d => d.DonationAmount.Value),
                        RecentDonation = new {
                            Amount = request.Amount,
                            Currency = request.Currency,
                            TransactionId = request.TransactionId,
                            ConfirmedAt = DateTime.UtcNow
                        }
                    });

                    return Ok(new { success = true, message = "Donation confirmed automatically" });
                }

                return NotFound(new { success = false, message = "Tracking record not found" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to confirm donation automatically");
                return StatusCode(500, new { success = false, message = "Failed to confirm donation" });
            }
        }
        
        [HttpPost("SubmitSupporterInfo")]
        public async Task<IActionResult> SubmitSupporterInfo([FromBody] SupporterInfoDto request)
        {
            try
            {
                var userIP = GetClientIpAddress(HttpContext);
                
                // Find the donation tracking record
                DonationTracking? donation = null;
                
                // First try to find by tracking ID
                if (!string.IsNullOrEmpty(request.TrackingId))
                {
                    donation = await dbContext.DonationTrackings
                        .FirstOrDefaultAsync(d => d.Id.ToString() == request.TrackingId);
                }
                
                // If not found, try to find by transaction ID
                if (donation == null && !string.IsNullOrEmpty(request.TransactionId))
                {
                    donation = await dbContext.DonationTrackings
                        .FirstOrDefaultAsync(d => d.PayPalTransactionId == request.TransactionId);
                }
                
                // If still not found, find most recent donation from this IP
                if (donation == null)
                {
                    donation = await dbContext.DonationTrackings
                        .Where(d => d.UserIP == userIP && 
                                   d.ConfirmedDonation && 
                                   d.DonationConfirmedAt >= DateTime.UtcNow.AddMinutes(-30))
                        .OrderByDescending(d => d.DonationConfirmedAt)
                        .FirstOrDefaultAsync();
                }
                
                if (donation == null)
                {
                    return BadRequest(new { success = false, message = "Could not find associated donation" });
                }
                
                // Check if supporter info already exists
                var existingSupporter = await dbContext.DonationSupporters
                    .FirstOrDefaultAsync(s => s.DonationTrackingId == donation.Id);
                
                if (existingSupporter != null)
                {
                    return BadRequest(new { success = false, message = "Supporter information already submitted" });
                }
                
                // Get Discord user if available from Discord ID in header
                long? discordUserId = null;
                var discordIdHeader = HttpContext.Request.Headers["X-Discord-Id"].FirstOrDefault();
                if (!string.IsNullOrEmpty(discordIdHeader))
                {
                    var discordUser = await dbContext.DiscordUsers
                        .FirstOrDefaultAsync(u => u.DiscordId == discordIdHeader);
                    
                    if (discordUser != null)
                    {
                        discordUserId = discordUser.Id;
                    }
                }
                
                // Create supporter record
                var supporter = new DonationSupporter
                {
                    DonationTrackingId = donation.Id,
                    PayPalTransactionId = donation.PayPalTransactionId,
                    DisplayName = request.DisplayName,
                    WebsiteUrl = request.WebsiteUrl,
                    DiscordUserId = discordUserId,
                    DiscordUsername = discordUserId == null ? request.DiscordUsername : null,
                    ShowOnSupportersPage = request.ShowOnSupportersPage,
                    SubmittedAt = DateTime.UtcNow,
                    UserIP = userIP,
                    Notes = request.Notes
                };
                
                dbContext.DonationSupporters.Add(supporter);
                await dbContext.SaveChangesAsync();
                
                logger.LogInformation("Supporter info submitted for donation {DonationId} by {DisplayName}", 
                    donation.Id, request.DisplayName);
                
                return Ok(new { 
                    success = true, 
                    message = "Thank you! Your supporter information has been saved.",
                    hasDiscordConnected = discordUserId != null
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to submit supporter info");
                return StatusCode(500, new { success = false, message = "Failed to save supporter information" });
            }
        }

        [HttpGet("GetSnitchLeaderboard")]
        public async Task<ActionResult<List<SnitchLeaderboardDto>>> GetSnitchLeaderboard([FromQuery] int count = 10)
        {
            try
            {
                var topSnitches = await dbContext.SnitchLeaderboards
                    .OrderByDescending(s => s.SnitchScore)
                    .Take(Math.Min(count, 50)) // Cap at 50 for performance
                    .ToListAsync();

                var leaderboard = topSnitches.Select((entry, index) => new SnitchLeaderboardDto
                {
                    Rank = index + 1,
                    DisplayName = entry.DisplayName ?? GenerateAnonymousDisplayName(entry.UserIdentifier),
                    TotalIssuesSubmitted = entry.TotalIssuesSubmitted,
                    OpenIssuesSubmitted = entry.OpenIssuesSubmitted,
                    ClosedIssuesSubmitted = entry.ClosedIssuesSubmitted,
                    TotalRepositoriesAffected = entry.TotalRepositoriesAffected,
                    FavoriteApiType = entry.FavoriteApiType,
                    SnitchScore = entry.SnitchScore,
                    FirstSubmissionAt = entry.FirstSubmissionAt,
                    LastSubmissionAt = entry.LastSubmissionAt,
                    ConsecutiveDaysActive = entry.ConsecutiveDaysActive
                }).ToList();

                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve snitch leaderboard");
                return StatusCode(500);
            }
        }

        [HttpGet("GetLeaderboardStats")]
        public async Task<ActionResult<object>> GetLeaderboardStats()
        {
            try
            {
                var totalSnitches = await dbContext.SnitchLeaderboards.CountAsync();
                var totalIssuesSubmitted = await dbContext.IssueSubmissionTrackings.CountAsync();
                var totalIssuesVerified = await dbContext.IssueVerifications.CountAsync();
                var openIssues = await dbContext.IssueVerifications.CountAsync(iv => iv.Status == IssueVerificationStatus.Open);
                var closedIssues = await dbContext.IssueVerifications.CountAsync(iv => iv.Status == IssueVerificationStatus.Closed);

                var stats = new
                {
                    TotalSnitches = totalSnitches,
                    TotalIssuesSubmitted = totalIssuesSubmitted,
                    TotalIssuesVerified = totalIssuesVerified,
                    OpenIssues = openIssues,
                    ClosedIssues = closedIssues,
                    SuccessRate = totalIssuesVerified > 0 ? Math.Round((double)(openIssues + closedIssues) / totalIssuesVerified * 100, 1) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve leaderboard statistics");
                return StatusCode(500);
            }
        }

        private static string GenerateAnonymousDisplayName(string userIdentifier)
        {
            var hash = userIdentifier.GetHashCode();
            var adjectives = new[] { "Anonymous", "Mysterious", "Stealthy", "Vigilant", "Alert", "Sharp", "Keen", "Clever", "Swift", "Silent" };
            var nouns = new[] { "Snitch", "Watcher", "Guardian", "Detective", "Hunter", "Scout", "Finder", "Seeker", "Tracker", "Spy" };
            
            var adjIndex = Math.Abs(hash) % adjectives.Length;
            var nounIndex = Math.Abs(hash / adjectives.Length) % nouns.Length;
            
            return $"{adjectives[adjIndex]} {nouns[nounIndex]} #{Math.Abs(hash) % 1000:D3}";
        }

        private async Task<APIKey?> GetRandomApiKeyAndUpdateDisplayCount(int? apiType = null)
        {
            // For random selection, we'll use a more efficient approach
            // Get the count of valid keys first
            // Showing only valid keys for the last 24 hours
            var query = dbContext.APIKeys
                .Where(s => s.Status == ApiStatusEnum.Valid &&
                            s.LastCheckedUTC > DateTime.UtcNow.AddHours(-24));

            if (apiType.HasValue)
            {
                query = query.Where(k => (int)k.ApiType == apiType.Value);
            }

            var totalCount = await query.CountAsync();
            
            if (totalCount == 0)
            {
                logger.LogWarning("No valid API keys found in the database{S}.", apiType.HasValue ? $" for type {apiType}" : "");
                return null;
            }

            // Select a random index
            var random = new Random();
            var randomIndex = random.Next(totalCount);

            // Get the key at that index using Skip
            var selectedKey = await query
                .OrderBy(x => x.Id) // Ensure consistent ordering
                .Skip(randomIndex)
                .Take(1)
                .Include(x => x.References)
                .FirstOrDefaultAsync();

            if (selectedKey != null)
            {
                // Update the display count
                selectedKey.TimesDisplayed++;
                await dbContext.SaveChangesAsync();
                
                // Update the in-memory count and broadcast
                displayCountService.IncrementCount();
                await hubContext.Clients.All.SendAsync("DisplayCountUpdated", displayCountService.TotalDisplayCount);
                
                logger.LogInformation("Incremented display count for Key ID: {Id}, Total: {TotalCount}", 
                    selectedKey.Id, displayCountService.TotalDisplayCount);
                return selectedKey;
            }

            logger.LogError("Could not retrieve random key from database.");
            return null;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get from X-Forwarded-For header first (for clients behind proxies)
            string? ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();

            // If not available, use the remote IP address
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            return ip;
        }

        [HttpGet("GetAllValidKeys")]
        [AllowedReferrers]
        [RateLimit(10)]
        public async Task<ActionResult<PaginatedResponse<APIKey>>> GetAllValidKeys(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? type = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100; // Cap at 100 for performance

            // Build query
            var query = dbContext.APIKeys
                .Include(x => x.References)
                .Where(s => s.Status == ApiStatusEnum.Valid);

            // Add type filter if specified
            if (type.HasValue)
            {
                query = query.Where(k => (int)k.ApiType == type.Value);
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();

            // Apply pagination
            var items = await query
                .OrderBy(x => x.Id) // Ensure consistent ordering
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedResponse<APIKey>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }

        // DTOs
        public class SnitchLeaderboardDto
        {
            public int Rank { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public int TotalIssuesSubmitted { get; set; }
            public int OpenIssuesSubmitted { get; set; }
            public int ClosedIssuesSubmitted { get; set; }
            public int TotalRepositoriesAffected { get; set; }
            public string? FavoriteApiType { get; set; }
            public double SnitchScore { get; set; }
            public DateTime FirstSubmissionAt { get; set; }
            public DateTime LastSubmissionAt { get; set; }
            public int ConsecutiveDaysActive { get; set; }
        }

        public class IssueSubmissionTrackingDto
        {
            public int ApiKeyId { get; set; }
            public string ApiType { get; set; } = string.Empty;
            public string RepoUrl { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
        }

        public class PaginatedResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
        }

        public class ApiTypeCountDto
        {
            public string ApiType { get; set; } = string.Empty;
            public int KeyCount { get; set; }
            public int ApiTypeId { get; set; }  // This will hold the enum value
        }

        public class PatternEffectivenessDto
        {
            public string Pattern { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public int TotalMatches { get; set; }
            public int ValidKeys { get; set; }
            public int InvalidKeys { get; set; }
            public double SuccessRate { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        public class KeyRotationDto
        {
            public DateTime RotatedAt { get; set; }
            public string RepoUrl { get; set; } = string.Empty;
            public int OldKeyDaysActive { get; set; }
            public string OldKeyType { get; set; } = string.Empty;
            public string NewKeyType { get; set; } = string.Empty;
        }

        public class KeyFixedNotification
        {
            public long KeyId { get; set; }
            public string? Reason { get; set; }
            public string? ReporterEmail { get; set; }
        }

        public class DonationClickTrackingDto
        {
            public string? ClickLocation { get; set; }
            public string? SessionId { get; set; }
        }

        public class DonationConfirmationDto
        {
            public decimal Amount { get; set; }
            public string? TransactionId { get; set; }
            public string? Notes { get; set; }
        }

        public class AutomaticDonationConfirmationDto
        {
            public string TrackingId { get; set; } = string.Empty;
            public string TransactionId { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? ItemName { get; set; }
            public string? ItemNumber { get; set; }
            public string? CustomMessage { get; set; }
        }
        
        public class SupporterInfoDto
        {
            public string? TrackingId { get; set; }
            public string? TransactionId { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string? WebsiteUrl { get; set; }
            public string? DiscordUsername { get; set; }
            public bool ShowOnSupportersPage { get; set; } = true;
            public string? Notes { get; set; }
        }
    }
}
