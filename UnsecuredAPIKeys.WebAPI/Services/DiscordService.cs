using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.WebAPI.Services
{
    public interface IDiscordService
    {
        Task<DiscordUser?> GetOrCreateUserAsync(string discordId, string accessToken, string refreshToken, DateTime tokenExpiresAt, string? ipAddress = null);
        Task<bool> VerifyServerMembershipAsync(string discordId, string accessToken);
        Task<DiscordUser?> GetUserByIdAsync(string discordId);
        Task<int?> GetUserRateLimitAsync(string discordId);
        Task RefreshUserTokenIfNeededAsync(DiscordUser user);
        Task<string> GetAuthorizationUrlAsync();
    }

    public class DiscordService(
        HttpClient httpClient,
        DBContext dbContext,
        ILogger<DiscordService> logger,
        IConfiguration configuration,
        IDiscordRoleService discordRoleService)
        : IDiscordService
    {
        private readonly IConfiguration _configuration = configuration;

        // Your Discord server ID - you'll need to set this
        private readonly string _serverId = string.Empty;
        private readonly string _clientId = string.Empty;
        private readonly string _clientSecret = string.Empty;
        private readonly string _redirectUri = string.Empty;

        // These will be loaded from database on demand

        private async Task<(string serverId, string clientId, string clientSecret, string redirectUri)> GetDiscordSettingsAsync()
        {
            var settings = await dbContext.ApplicationSettings
                .Where(s => s.Key.StartsWith("Discord"))
                .ToListAsync();

            var serverId = settings.FirstOrDefault(s => s.Key == "DiscordServerId")?.Value 
                ?? throw new InvalidOperationException("DiscordServerId not configured in ApplicationSettings");
            var clientId = settings.FirstOrDefault(s => s.Key == "DiscordClientId")?.Value 
                ?? throw new InvalidOperationException("DiscordClientId not configured in ApplicationSettings");
            var clientSecret = settings.FirstOrDefault(s => s.Key == "DiscordClientSecret")?.Value 
                ?? throw new InvalidOperationException("DiscordClientSecret not configured in ApplicationSettings");
            var redirectUri = settings.FirstOrDefault(s => s.Key == "DiscordRedirectUri")?.Value 
                ?? throw new InvalidOperationException("DiscordRedirectUri not configured in ApplicationSettings");

            return (serverId, clientId, clientSecret, redirectUri);
        }

        public async Task<string> GetAuthorizationUrlAsync()
        {
            var (_, clientId, _, redirectUri) = await GetDiscordSettingsAsync();
            
            var scopes = "identify guilds guilds.members.read";
            var state = Guid.NewGuid().ToString(); // You might want to store this for validation
            
            return $"https://discord.com/api/oauth2/authorize?" +
                   $"client_id={clientId}" +
                   $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopes)}" +
                   $"&state={state}";
        }

        public async Task<DiscordUser?> GetOrCreateUserAsync(string discordId, string accessToken, string refreshToken, DateTime tokenExpiresAt, string? ipAddress = null)
        {
            try
            {
                // Get user info from Discord API
                var userInfo = await GetDiscordUserInfoAsync(accessToken);
                if (userInfo == null)
                {
                    return null;
                }

                // Check if user exists in our database
                var existingUser = await dbContext.DiscordUsers
                    .FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (existingUser != null)
                {
                    // Update existing user
                    existingUser.Username = userInfo.Username;
                    existingUser.Discriminator = userInfo.Discriminator;
                    existingUser.Avatar = userInfo.Avatar;
                    existingUser.Email = userInfo.Email;
                    existingUser.AccessToken = accessToken; // In production, encrypt this
                    existingUser.RefreshToken = refreshToken; // In production, encrypt this
                    existingUser.TokenExpiresAt = tokenExpiresAt;
                    existingUser.LastLoginUtc = DateTime.UtcNow;
                    existingUser.LastSeenUtc = DateTime.UtcNow;

                    // Update IP address if provided
                    if (!string.IsNullOrEmpty(ipAddress))
                    {
                        existingUser.LastKnownIpAddress = ipAddress;
                        existingUser.IpLastUpdatedUtc = DateTime.UtcNow;
                    }

                    // Check server membership
                    existingUser.IsServerMember = await VerifyServerMembershipAsync(discordId, accessToken);
                    existingUser.LastMembershipCheckUtc = DateTime.UtcNow;

                    await dbContext.SaveChangesAsync();
                    
                    // Update roles if user is a server member
                    if (existingUser.IsServerMember)
                    {
                        await discordRoleService.UpdateUserRolesAsync(discordId);
                        // Reload the user to get updated role info
                        existingUser = await dbContext.DiscordUsers
                            .FirstOrDefaultAsync(u => u.DiscordId == discordId) ?? existingUser;
                    }
                    
                    return existingUser;
                }
                else
                {
                    // Create new user
                    var isServerMember = await VerifyServerMembershipAsync(discordId, accessToken);
                    
                    var newUser = new DiscordUser
                    {
                        DiscordId = discordId,
                        Username = userInfo.Username,
                        Discriminator = userInfo.Discriminator,
                        Avatar = userInfo.Avatar,
                        Email = userInfo.Email,
                        IsServerMember = isServerMember,
                        FirstLoginUtc = DateTime.UtcNow,
                        LastLoginUtc = DateTime.UtcNow,
                        LastMembershipCheckUtc = DateTime.UtcNow,
                        AccessToken = accessToken, // In production, encrypt this
                        RefreshToken = refreshToken, // In production, encrypt this
                        TokenExpiresAt = tokenExpiresAt,
                        LastSeenUtc = DateTime.UtcNow,
                        LastKnownIpAddress = ipAddress,
                        IpLastUpdatedUtc = !string.IsNullOrEmpty(ipAddress) ? DateTime.UtcNow : null,
                        RateLimitOverride = isServerMember ? await GetDiscordMemberRateLimitAsync() : null
                    };

                    dbContext.DiscordUsers.Add(newUser);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation("Created new Discord user: {Username}#{Discriminator} (Member: {IsServerMember}) IP: {IpAddress}",
                        userInfo.Username, userInfo.Discriminator, isServerMember, ipAddress ?? "Unknown");
                    
                    // Update roles if user is a server member
                    if (isServerMember)
                    {
                        await discordRoleService.UpdateUserRolesAsync(discordId);
                        // Reload the user to get updated role info
                        newUser = await dbContext.DiscordUsers
                            .FirstOrDefaultAsync(u => u.DiscordId == discordId) ?? newUser;
                    }
                    
                    return newUser;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting or creating Discord user: {DiscordId}", discordId);
                return null;
            }
        }

        public async Task<bool> VerifyServerMembershipAsync(string discordId, string accessToken)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.GetAsync("https://discord.com/api/users/@me/guilds");
                
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Failed to get user guilds for Discord ID {DiscordId}: {StatusCode}",
                        discordId, response.StatusCode);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var guilds = JsonSerializer.Deserialize<DiscordGuild[]>(content);

                if (guilds == null)
                {
                    return false;
                }

                var (serverId, _, _, _) = await GetDiscordSettingsAsync();
                return guilds.Any(g => g.Id == serverId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error verifying server membership for Discord ID {DiscordId}", discordId);
                return false;
            }
        }

        public async Task<DiscordUser?> GetUserByIdAsync(string discordId)
        {
            return await dbContext.DiscordUsers
                .FirstOrDefaultAsync(u => u.DiscordId == discordId);
        }

        public async Task<int?> GetUserRateLimitAsync(string discordId)
        {
            var user = await GetUserByIdAsync(discordId);
            
            if (user?.IsServerMember == true)
            {
                return user.RateLimitOverride ?? await GetDiscordMemberRateLimitAsync();
            }

            return null; // No rate limit override
        }

        public async Task RefreshUserTokenIfNeededAsync(DiscordUser user)
        {
            if (user.TokenExpiresAt == null || user.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return; // Token is still valid
            }

            try
            {
                // Refresh the token using the refresh token
                var refreshResponse = await RefreshAccessTokenAsync(user.RefreshToken!);
                if (refreshResponse != null)
                {
                    user.AccessToken = refreshResponse.AccessToken;
                    user.RefreshToken = refreshResponse.RefreshToken;
                    user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(refreshResponse.ExpiresIn);
                    
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Refreshed token for Discord user: {DiscordId}", user.DiscordId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing token for Discord user: {DiscordId}", user.DiscordId);
            }
        }

        private async Task<DiscordUserInfo?> GetDiscordUserInfoAsync(string accessToken)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.GetAsync("https://discord.com/api/users/@me");
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DiscordUserInfo>(content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting Discord user info");
                return null;
            }
        }

        private async Task<DiscordTokenResponse?> RefreshAccessTokenAsync(string refreshToken)
        {
            try
            {
                var (_, clientId, clientSecret, _) = await GetDiscordSettingsAsync();
                
                var formData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                });

                var response = await httpClient.PostAsync("https://discord.com/api/oauth2/token", formData);
                
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DiscordTokenResponse>(content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing Discord access token");
                return null;
            }
        }

        private async Task<int> GetDiscordMemberRateLimitAsync()
        {
            var setting = await dbContext.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "DiscordMemberRateLimit");
            
            if (setting != null && int.TryParse(setting.Value, out var limit))
            {
                return limit;
            }
            
            return 20; // Default fallback
        }
    }

    // DTOs for Discord API responses
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
        
        [JsonPropertyName("global_name")]
        public string? GlobalName { get; set; }
    }

    public class DiscordGuild
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
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
}
