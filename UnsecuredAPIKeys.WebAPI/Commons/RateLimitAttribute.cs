using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.WebAPI.Hubs;
using UnsecuredAPIKeys.WebAPI.Models;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI.Commons
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute(int maxRequests = 0, int timeWindowHours = 1) : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var ipAddress = GetClientIpAddress(context);
            var endpoint = context.HttpContext.Request.Path.Value ?? "";
            
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<DBContext>();
            
            var effectiveMaxRequests = maxRequests;
            
            // Check for Discord user first (they get priority rate limits)
            var discordId = context.HttpContext.Request.Headers["X-Discord-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(discordId))
            {
                var discordRoleService = context.HttpContext.RequestServices.GetRequiredService<IDiscordRoleService>();
                
                // Get role-based rate limit
                var discordRateLimit = await discordRoleService.GetUserRateLimitAsync(discordId);
                if (discordRateLimit > 0)
                {
                    effectiveMaxRequests = discordRateLimit;
                }
            }
            
            // If no Discord override and maxRequests is 0 (default), read from ApplicationSettings
            if (effectiveMaxRequests == 0)
            {
                var rateLimitSetting = await dbContext.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == "RateLimit");
                
                if (rateLimitSetting != null && int.TryParse(rateLimitSetting.Value, out var parsedLimit))
                {
                    effectiveMaxRequests = parsedLimit;
                }
                else
                {
                    effectiveMaxRequests = 5; // Final fallback
                }
            }
            // Otherwise, use the provided value
            
            // Calculate the time window
            var timeWindowStart = DateTime.UtcNow.AddHours(-timeWindowHours);
            
            // Count requests from this IP for this endpoint within time window
            var requestCount = await dbContext.RateLimitLogs
                .Where(r => r.IpAddress == ipAddress && 
                            r.Endpoint == endpoint && 
                            r.RequestTimeUtc >= timeWindowStart)
                .CountAsync();
            
            // Calculate the reset time (when the time window resets)
            var oldestLogTime = DateTime.UtcNow;
            if (requestCount > 0)
            {
                oldestLogTime = await dbContext.RateLimitLogs
                    .Where(r => r.IpAddress == ipAddress && r.Endpoint == endpoint)
                    .OrderBy(r => r.RequestTimeUtc)
                    .Select(r => r.RequestTimeUtc)
                    .FirstOrDefaultAsync();
            }
            var resetAt = oldestLogTime.AddHours(timeWindowHours);
            
            if (requestCount >= effectiveMaxRequests)
            {
                // Limit exceeded
                var rateLimitInfo = new RateLimitResponse.RateLimitInfo
                {
                    Limit = effectiveMaxRequests,
                    RequestsRemaining = 0,
                    RequestsCount = requestCount,
                    TimeWindow = TimeSpan.FromHours(timeWindowHours),
                    ResetAt = resetAt
                };

                // Try to get the requested API type from query parameters
                APIKey? validApiKey = null;
                var typeParam = context.HttpContext.Request.Query["type"].FirstOrDefault();
                
                if (!string.IsNullOrEmpty(typeParam) && int.TryParse(typeParam, out var apiType))
                {
                    // First try to get a key of the requested type WITH references
                    validApiKey = await dbContext.APIKeys
                        .Include(x => x.References)
                        .Where(x => x.Status == ApiStatusEnum.Valid && 
                                   (int)x.ApiType == apiType && 
                                   x.References.Any())
                        .OrderBy(x => x.Id)  // Ensure consistent "punishment" key
                        .FirstOrDefaultAsync();
                    
                    // If no key with references found, try without the reference requirement
                    if (validApiKey == null)
                    {
                        validApiKey = await dbContext.APIKeys
                            .Include(x => x.References)
                            .Where(x => x.Status == ApiStatusEnum.Valid && (int)x.ApiType == apiType)
                            .OrderBy(x => x.Id)
                            .FirstOrDefaultAsync();
                    }
                }
                
                // If no key of the requested type was found (or no type was specified), get any valid key WITH references
                if (validApiKey == null)
                {
                    validApiKey = await dbContext.APIKeys
                        .Include(x => x.References)
                        .Where(x => x.Status == ApiStatusEnum.Valid && x.References.Any())
                        .OrderBy(x => x.Id)  // Ensure consistent "punishment" key
                        .FirstOrDefaultAsync();
                    
                    // Last resort: any valid key
                    if (validApiKey == null)
                    {
                        validApiKey = await dbContext.APIKeys
                            .Include(x => x.References)
                            .Where(x => x.Status == ApiStatusEnum.Valid)
                            .OrderBy(x => x.Id)
                            .FirstOrDefaultAsync();
                    }
                }

                if (validApiKey != null)  // Always good to null check
                {
                    validApiKey.TimesDisplayed++;
                    await dbContext.SaveChangesAsync();
                    
                    // Update the in-memory count and broadcast
                    var displayCountService = context.HttpContext.RequestServices.GetRequiredService<IDisplayCountService>();
                    var hubContext = context.HttpContext.RequestServices.GetRequiredService<IHubContext<StatsHub>>();
                    
                    displayCountService.IncrementCount();
                    await hubContext.Clients.All.SendAsync("DisplayCountUpdated", displayCountService.TotalDisplayCount);
                }

                var response = new RateLimitResponse
                {
                    Success = false,
                    Message = $"Rate limit exceeded. Maximum {effectiveMaxRequests} requests per {timeWindowHours} hour(s) allowed.",
                    RateLimit = rateLimitInfo,
                    FallbackApiKey = validApiKey
                };
                
                context.Result = new JsonResult(response)
                {
                    StatusCode = 429 // Too Many Requests
                };
                return;
            }
            
            // Log this request
            var now = DateTime.UtcNow;
            dbContext.RateLimitLogs.Add(new RateLimitLog
            {
                IpAddress = ipAddress,
                Endpoint = endpoint,
                RequestTimeUtc = now
            });
            await dbContext.SaveChangesAsync();
            
            // Add rate limit headers to the response
            var remainingRequests = effectiveMaxRequests - (requestCount + 1);
            var resetTime = oldestLogTime.AddHours(timeWindowHours);
            
            context.HttpContext.Response.Headers["X-RateLimit-Limit"] = effectiveMaxRequests.ToString();
            context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = remainingRequests.ToString();
            context.HttpContext.Response.Headers["X-RateLimit-Reset"] = resetTime.ToString("o");
            
            // Execute the action
            await next();
        }

        private string GetClientIpAddress(ActionExecutingContext context)
        {
            // Try to get from X-Forwarded-For header first (for clients behind proxies)
            string? ip = context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
            
            // If not available, use the remote IP address
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }
            
            return ip;
        }
    }
}
