using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiscordAuthController(
        IDiscordService discordService,
        ILogger<DiscordAuthController> logger,
        IConfiguration configuration)
        : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;

        [HttpGet("login")]
        public async Task<ActionResult> GetLoginUrl()
        {
            try
            {
                var authUrl = await discordService.GetAuthorizationUrlAsync();
                return Ok(new { authUrl });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating Discord auth URL");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("callback")]
        public async Task<ActionResult> HandleCallback([FromBody] DiscordCallbackRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Code))
                {
                    logger.LogWarning("Discord callback received without authorization code");
                    return BadRequest(new { 
                        success = false, 
                        error = "Authorization code is required",
                        message = "Authorization code is required" 
                    });
                }

                logger.LogInformation("Processing Discord OAuth callback for code: {CodePrefix}...", 
                    request.Code.Substring(0, Math.Min(8, request.Code.Length)));

                // Exchange code for access token
                var tokenResponse = await ExchangeCodeForTokenAsync(request.Code);
                if (tokenResponse == null)
                {
                    logger.LogError("Token exchange failed - see previous logs for details");
                    return BadRequest(new { 
                        success = false, 
                        error = "Failed to exchange code for token",
                        message = "Failed to exchange code for token" 
                    });
                }

                logger.LogInformation("Successfully exchanged code for Discord access token");

                // Get user info from Discord API
                var userInfo = await GetDiscordUserInfoAsync(tokenResponse.AccessToken);
                if (userInfo == null)
                {
                    logger.LogError("Failed to get user info from Discord API");
                    return BadRequest(new { 
                        success = false, 
                        error = "Failed to get user information from Discord",
                        message = "Failed to get user information from Discord" 
                    });
                }

                logger.LogInformation("Successfully retrieved Discord user info for {Username} ({Id})", 
                    userInfo.Username, userInfo.Id);

                // Get client IP address
                var ipAddress = GetClientIpAddress();
                
                // Create or update user in our database
                var tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
                var discordUser = await discordService.GetOrCreateUserAsync(
                    userInfo.Id, 
                    tokenResponse.AccessToken, 
                    tokenResponse.RefreshToken, 
                    tokenExpiresAt, 
                    ipAddress);

                if (discordUser == null)
                {
                    logger.LogError("Failed to create/update user record in database");
                    return StatusCode(500, new { 
                        success = false, 
                        error = "Failed to create user record",
                        message = "Failed to create user record" 
                    });
                }

                logger.LogInformation("Successfully processed Discord authentication for {Username} (Member: {IsServerMember})", 
                    discordUser.Username, discordUser.IsServerMember);

                var response = new DiscordAuthResponse
                {
                    Success = true,
                    User = new DiscordUserResponse
                    {
                        DiscordId = discordUser.DiscordId,
                        Username = discordUser.Username,
                        Discriminator = discordUser.Discriminator,
                        Avatar = discordUser.Avatar,
                        IsServerMember = discordUser.IsServerMember,
                        RateLimitOverride = discordUser.RateLimitOverride
                    },
                    Message = discordUser.IsServerMember 
                        ? $"Welcome back, {discordUser.Username}! You have enhanced rate limits as a server member."
                        : $"Hello, {discordUser.Username}! Join our Discord server for enhanced rate limits."
                };

                // Always return success response with HTTP 200, even if there were non-critical issues
                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error processing Discord OAuth callback");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Internal server error",
                    message = "An unexpected error occurred during authentication" 
                });
            }
        }

        [HttpGet("user/{discordId}")]
        public async Task<ActionResult> GetUserInfo(string discordId)
        {
            try
            {
                var user = await discordService.GetUserByIdAsync(discordId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                var response = new DiscordUserResponse
                {
                    DiscordId = user.DiscordId,
                    Username = user.Username,
                    Discriminator = user.Discriminator,
                    Avatar = user.Avatar,
                    IsServerMember = user.IsServerMember,
                    RateLimitOverride = user.RateLimitOverride
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Discord user info for {DiscordId}", discordId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("refresh-membership/{discordId}")]
        public async Task<ActionResult> RefreshMembership(string discordId)
        {
            try
            {
                var user = await discordService.GetUserByIdAsync(discordId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                // Refresh token if needed
                await discordService.RefreshUserTokenIfNeededAsync(user);

                // Check current membership status
                var isServerMember = await discordService.VerifyServerMembershipAsync(user.DiscordId, user.AccessToken!);
                
                user.IsServerMember = isServerMember;
                user.LastMembershipCheckUtc = DateTime.UtcNow;
                user.RateLimitOverride = isServerMember ? user.RateLimitOverride ?? 20 : null;
                
                // Save to database (assuming you have a way to save the context)
                // This would typically be done through the service
                
                var response = new DiscordUserResponse
                {
                    DiscordId = user.DiscordId,
                    Username = user.Username,
                    Discriminator = user.Discriminator,
                    Avatar = user.Avatar,
                    IsServerMember = user.IsServerMember,
                    RateLimitOverride = user.RateLimitOverride
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing membership for Discord user {DiscordId}", discordId);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private async Task<DiscordTokenResponse?> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                using var httpClient = new HttpClient();
                using var scope = HttpContext.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<UnsecuredAPIKeys.Data.DBContext>();
                
                var settings = await dbContext.ApplicationSettings
                    .Where(s => s.Key.StartsWith("Discord"))
                    .ToListAsync();

                var clientId = settings.FirstOrDefault(s => s.Key == "DiscordClientId")?.Value 
                    ?? throw new InvalidOperationException("DiscordClientId not configured in ApplicationSettings");
                var clientSecret = settings.FirstOrDefault(s => s.Key == "DiscordClientSecret")?.Value 
                    ?? throw new InvalidOperationException("DiscordClientSecret not configured in ApplicationSettings");
                var redirectUri = settings.FirstOrDefault(s => s.Key == "DiscordRedirectUri")?.Value 
                    ?? throw new InvalidOperationException("DiscordRedirectUri not configured in ApplicationSettings");

                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri)
                });

                var response = await httpClient.PostAsync("https://discord.com/api/oauth2/token", formData);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogWarning("Discord token exchange failed: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<DiscordTokenResponse>(content, options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exchanging Discord code for token");
                return null;
            }
        }

        private async Task<DiscordUserInfo?> GetDiscordUserInfoAsync(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.GetAsync("https://discord.com/api/users/@me");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                 return JsonSerializer.Deserialize<DiscordUserInfo>(content, options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Discord user info");
                return null;
            }
        }

        private string GetClientIpAddress()
        {
            // Check for forwarded IP headers first (for load balancers/proxies)
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to direct connection IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    // DTOs
    public class DiscordCallbackRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class DiscordAuthResponse
    {
        public bool Success { get; set; }
        public DiscordUserResponse? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class DiscordUserResponse
    {
        public string DiscordId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? Discriminator { get; set; }
        public string? Avatar { get; set; }
        public bool IsServerMember { get; set; }
        public int? RateLimitOverride { get; set; }
    }

    public class DiscordTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
        
        [JsonPropertyName("scope")]
        public string Scope { get; set; } = string.Empty;
    }

    public class DiscordUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("discriminator")]
        public string? Discriminator { get; set; }
        
        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
