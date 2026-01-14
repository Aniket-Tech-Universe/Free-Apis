using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.WebAPI.Services
{
    public interface IDiscordRoleService
    {
        Task<int> GetUserRateLimitAsync(string discordId);
        Task UpdateUserRolesAsync(string discordId);
        Task<List<string>> GetUserRolesAsync(string discordId);
    }

    public class DiscordRoleService(
        HttpClient httpClient,
        DBContext dbContext,
        ILogger<DiscordRoleService> logger)
        : IDiscordRoleService
    {
        public async Task<int> GetUserRateLimitAsync(string discordId)
        {
            try
            {
                var user = await dbContext.DiscordUsers
                    .FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (user?.IsServerMember != true)
                {
                    // Not a server member, use default rate limit
                    var defaultSetting = await dbContext.ApplicationSettings
                        .FirstOrDefaultAsync(s => s.Key == "RateLimit");
                    return int.TryParse(defaultSetting?.Value, out var defaultLimit) ? defaultLimit : 5;
                }

                // Check if roles need updating (older than 1 hour)
                if (user.LastRoleCheckUtc == null || 
                    user.LastRoleCheckUtc < DateTime.UtcNow.AddHours(-1))
                {
                    await UpdateUserRolesAsync(discordId);
                    // Reload user after role update
                    user = await dbContext.DiscordUsers
                        .FirstOrDefaultAsync(u => u.DiscordId == discordId);
                }

                // Get the highest rate limit based on roles
                return await CalculateRateLimitFromRoles(user);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting rate limit for Discord user {DiscordId}", discordId);
                return 5; // Default fallback
            }
        }

        public async Task UpdateUserRolesAsync(string discordId)
        {
            try
            {
                var user = await dbContext.DiscordUsers
                    .FirstOrDefaultAsync(u => u.DiscordId == discordId);

                if (user == null || !user.IsServerMember || string.IsNullOrEmpty(user.AccessToken))
                {
                    return;
                }

                var serverIdSetting = await dbContext.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == "DiscordServerId");

                if (serverIdSetting == null)
                {
                    logger.LogWarning("Discord server ID not configured");
                    return;
                }

                // Get user's roles from Discord API using their OAuth token
                var roles = await GetUserRolesFromDiscordAsync(serverIdSetting.Value, discordId, user.AccessToken);

                // Update user's roles
                user.ServerRoles = string.Join(",", roles);
                user.LastRoleCheckUtc = DateTime.UtcNow;
                user.HighestTier = await DetermineHighestTierAsync(roles);
                user.RateLimitOverride = await CalculateRateLimitFromRoles(user);

                await dbContext.SaveChangesAsync();

                logger.LogInformation("Updated roles for Discord user {DiscordId}: {Roles}", 
                    discordId, user.ServerRoles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating roles for Discord user {DiscordId}", discordId);
            }
        }

        public async Task<List<string>> GetUserRolesAsync(string discordId)
        {
            var user = await dbContext.DiscordUsers
                .FirstOrDefaultAsync(u => u.DiscordId == discordId);

            if (user?.ServerRoles == null)
            {
                return new List<string>();
            }

            return user.ServerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        private async Task<List<string>> GetUserRolesFromDiscordAsync(string serverId, string userId, string accessToken)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                // Use the correct endpoint to get the current user's member object for a specific guild
                var response = await httpClient.GetAsync($"https://discord.com/api/users/@me/guilds/{serverId}/member");

                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to get user member info from Discord API: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Discord API Error Response: {Error}", errorContent);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        logger.LogError("Access forbidden - user needs to re-authenticate with guilds.members.read scope");
                    }
                    
                    return new List<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Discord API Member Response: {Content}", content);
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var member = JsonSerializer.Deserialize<DiscordGuildMember>(content, options);

                if (member?.Roles != null)
                {
                    logger.LogInformation("Found roles for user {UserId} in guild {GuildId}: {Roles}", 
                        userId, serverId, string.Join(",", member.Roles));
                    return member.Roles;
                }

                logger.LogWarning("No roles found for user {UserId} in guild {GuildId}", userId, serverId);
                return new List<string>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching roles from Discord API");
                return new List<string>();
            }
        }

        private async Task<string?> DetermineHighestTierAsync(List<string> roleIds)
        {
            // Get role tier mappings from ApplicationSettings
            var roleTierSettings = await dbContext.ApplicationSettings
                .Where(s => s.Key.StartsWith("DiscordRoleTier:"))
                .ToListAsync();

            var userTiers = new List<string>();

            // Check which tiers the user has based on their role IDs
            foreach (var tierSetting in roleTierSettings)
            {
                // Extract tier name from key (e.g., "DiscordRoleTier:donator" -> "donator")
                var tierName = tierSetting.Key.Substring("DiscordRoleTier:".Length);
                
                // Check if user has the role ID for this tier
                if (roleIds.Contains(tierSetting.Value))
                {
                    userTiers.Add(tierName);
                }
            }

            // Return highest tier (priority order)
            if (userTiers.Contains("premium")) return "premium";
            if (userTiers.Contains("donator")) return "donator";
            if (userTiers.Contains("booster")) return "booster";
            if (userTiers.Contains("member")) return "member";

            return null;
        }

        private async Task<int> CalculateRateLimitFromRoles(DiscordUser user)
        {
            // First check for Site Creator role
            var siteCreatorRoleIdSetting = await dbContext.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "DiscordSiteCreatorRoleId");
            
            if (siteCreatorRoleIdSetting != null && !string.IsNullOrEmpty(user.ServerRoles))
            {
                var userRoles = user.ServerRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (userRoles.Contains(siteCreatorRoleIdSetting.Value))
                {
                    // Site Creator gets essentially unlimited rate limit
                    return 999999;
                }
            }

            if (user.HighestTier == null)
            {
                // Regular server member
                var memberLimitSetting = await dbContext.ApplicationSettings
                    .FirstOrDefaultAsync(s => s.Key == "DiscordMemberRateLimit");
                return int.TryParse(memberLimitSetting?.Value, out var memberLimit) ? memberLimit : 20;
            }

            // Get tier-specific rate limit
            var tierLimitSetting = await dbContext.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == $"DiscordTierRateLimit:{user.HighestTier}");

            if (tierLimitSetting != null && int.TryParse(tierLimitSetting.Value, out var tierLimit))
            {
                return tierLimit;
            }

            // Fallback to member rate limit
            var fallbackSetting = await dbContext.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "DiscordMemberRateLimit");
            return int.TryParse(fallbackSetting?.Value, out var fallbackLimit) ? fallbackLimit : 20;
        }
    }

    // Internal DTOs for Discord API responses - not exposed in OpenAPI
    internal class DiscordGuildMember
    {
        public string? Nick { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string? JoinedAt { get; set; }
        // Ignore the user object since we only need roles
        [JsonIgnore]
        public object? User { get; set; }
    }

    internal class DiscordGuildWithRoles
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string>? Roles { get; set; }
    }
}
