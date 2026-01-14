using Microsoft.EntityFrameworkCore;
using System.Net;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.Providers.Services
{
    /// <summary>
    /// Service for managing user moderation, bans, and IP tracking
    /// </summary>
    public class UserModerationService
    {
        private readonly DBContext _context;

        public UserModerationService(DBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Records a user session with IP tracking
        /// </summary>
        public async Task<UserSession> RecordUserSessionAsync(int? discordUserId, string ipAddress, string? userAgent = null, string? sessionId = null)
        {
            var now = DateTime.UtcNow;
            
            // Check if there's an existing active session for this user/IP combination
            var existingSession = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.DiscordUserId == discordUserId && 
                                         s.IpAddress == ipAddress && 
                                         s.IsActive);

            if (existingSession != null)
            {
                // Update existing session
                existingSession.LastSeenUtc = now;
                existingSession.RequestCount++;
                if (!string.IsNullOrEmpty(userAgent))
                    existingSession.UserAgent = userAgent;
                if (!string.IsNullOrEmpty(sessionId))
                    existingSession.SessionId = sessionId;
                
                await _context.SaveChangesAsync();
                return existingSession;
            }

            // Create new session
            var session = new UserSession
            {
                DiscordUserId = discordUserId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                FirstSeenUtc = now,
                LastSeenUtc = now,
                RequestCount = 1,
                SessionId = sessionId,
                IsActive = true
            };

            _context.UserSessions.Add(session);
            
            // Update Discord user's last known IP if applicable
            if (discordUserId.HasValue)
            {
                var discordUser = await _context.DiscordUsers.FindAsync(discordUserId.Value);
                if (discordUser != null)
                {
                    discordUser.LastKnownIpAddress = ipAddress;
                    discordUser.IpLastUpdatedUtc = now;
                    discordUser.LastSeenUtc = now;
                }
            }

            await _context.SaveChangesAsync();
            return session;
        }

        /// <summary>
        /// Checks if a user or IP is banned
        /// </summary>
        public async Task<(bool IsBanned, UserBan? Ban)> IsUserBannedAsync(int? discordUserId, string ipAddress)
        {
            var now = DateTime.UtcNow;
            
            var activeBans = await _context.UserBans
                .Where(b => b.IsActive && (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .ToListAsync();

            foreach (var ban in activeBans)
            {
                // Check Discord user ban
                if (ban.BanType == BanType.DiscordUser || ban.BanType == BanType.Both)
                {
                    if (discordUserId.HasValue && ban.DiscordUserId == discordUserId.Value)
                    {
                        return (true, ban);
                    }
                }

                // Check IP ban
                if (ban.BanType == BanType.IpAddress || ban.BanType == BanType.Both)
                {
                    if (ban.IpAddress == ipAddress)
                    {
                        return (true, ban);
                    }
                }

                // Check IP range ban
                if (ban.BanType == BanType.IpRange && !string.IsNullOrEmpty(ban.IpAddress) && ban.SubnetMask.HasValue)
                {
                    if (IsIpInRange(ipAddress, ban.IpAddress, ban.SubnetMask.Value))
                    {
                        return (true, ban);
                    }
                }
            }

            return (false, null);
        }

        /// <summary>
        /// Bans a Discord user
        /// </summary>
        public async Task<UserBan> BanDiscordUserAsync(int discordUserId, string reason, string bannedBy, DateTime? expiresAt = null, string? notes = null)
        {
            var ban = new UserBan
            {
                BanType = BanType.DiscordUser,
                DiscordUserId = discordUserId,
                Reason = reason,
                BannedBy = bannedBy,
                BannedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAt,
                IsActive = true,
                Notes = notes
            };

            _context.UserBans.Add(ban);
            await _context.SaveChangesAsync();
            return ban;
        }

        /// <summary>
        /// Bans an IP address
        /// </summary>
        public async Task<UserBan> BanIpAddressAsync(string ipAddress, string reason, string bannedBy, DateTime? expiresAt = null, string? notes = null)
        {
            var ban = new UserBan
            {
                BanType = BanType.IpAddress,
                IpAddress = ipAddress,
                Reason = reason,
                BannedBy = bannedBy,
                BannedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAt,
                IsActive = true,
                Notes = notes
            };

            _context.UserBans.Add(ban);
            await _context.SaveChangesAsync();
            return ban;
        }

        /// <summary>
        /// Bans both a Discord user and their known IP addresses
        /// </summary>
        public async Task<List<UserBan>> BanUserAndIpsAsync(int discordUserId, string reason, string bannedBy, DateTime? expiresAt = null, string? notes = null)
        {
            var bans = new List<UserBan>();

            // Ban the Discord user
            var userBan = await BanDiscordUserAsync(discordUserId, reason, bannedBy, expiresAt, notes);
            bans.Add(userBan);

            // Get all known IP addresses for this user
            var knownIps = await _context.UserSessions
                .Where(s => s.DiscordUserId == discordUserId)
                .Select(s => s.IpAddress)
                .Distinct()
                .ToListAsync();

            // Ban each IP address
            foreach (var ip in knownIps)
            {
                var ipBan = await BanIpAddressAsync(ip, $"Associated with banned user: {reason}", bannedBy, expiresAt, notes);
                bans.Add(ipBan);
            }

            return bans;
        }

        /// <summary>
        /// Bans an IP range using CIDR notation
        /// </summary>
        public async Task<UserBan> BanIpRangeAsync(string ipAddress, int subnetMask, string reason, string bannedBy, DateTime? expiresAt = null, string? notes = null)
        {
            var ban = new UserBan
            {
                BanType = BanType.IpRange,
                IpAddress = ipAddress,
                SubnetMask = subnetMask,
                Reason = reason,
                BannedBy = bannedBy,
                BannedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAt,
                IsActive = true,
                Notes = notes
            };

            _context.UserBans.Add(ban);
            await _context.SaveChangesAsync();
            return ban;
        }

        /// <summary>
        /// Removes a ban (unban)
        /// </summary>
        public async Task<bool> RemoveBanAsync(int banId, string removedBy, string? reason = null)
        {
            var ban = await _context.UserBans.FindAsync(banId);
            if (ban == null) return false;

            ban.IsActive = false;
            ban.UpdatedAtUtc = DateTime.UtcNow;
            ban.UpdatedBy = removedBy;
            if (!string.IsNullOrEmpty(reason))
            {
                ban.Notes = (ban.Notes ?? "") + $"\nUnbanned: {reason}";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets all IP addresses associated with a Discord user
        /// </summary>
        public async Task<List<string>> GetUserIpAddressesAsync(int discordUserId)
        {
            return await _context.UserSessions
                .Where(s => s.DiscordUserId == discordUserId)
                .Select(s => s.IpAddress)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Gets all Discord users associated with an IP address
        /// </summary>
        public async Task<List<DiscordUser>> GetUsersFromIpAddressAsync(string ipAddress)
        {
            return await _context.UserSessions
                .Where(s => s.IpAddress == ipAddress && s.DiscordUserId.HasValue)
                .Select(s => s.DiscordUser!)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Gets user session history
        /// </summary>
        public async Task<List<UserSession>> GetUserSessionHistoryAsync(int discordUserId, int limit = 50)
        {
            return await _context.UserSessions
                .Where(s => s.DiscordUserId == discordUserId)
                .OrderByDescending(s => s.LastSeenUtc)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Gets active bans for a user
        /// </summary>
        public async Task<List<UserBan>> GetActiveBansForUserAsync(int discordUserId)
        {
            var now = DateTime.UtcNow;
            return await _context.UserBans
                .Where(b => b.DiscordUserId == discordUserId && 
                           b.IsActive && 
                           (b.ExpiresAtUtc == null || b.ExpiresAtUtc > now))
                .ToListAsync();
        }

        /// <summary>
        /// Checks if an IP is within a CIDR range
        /// </summary>
        private bool IsIpInRange(string ipAddress, string rangeIp, int subnetMask)
        {
            try
            {
                var ip = IPAddress.Parse(ipAddress);
                var range = IPAddress.Parse(rangeIp);
                
                // Convert to bytes for comparison
                var ipBytes = ip.GetAddressBytes();
                var rangeBytes = range.GetAddressBytes();
                
                if (ipBytes.Length != rangeBytes.Length) return false;
                
                // Calculate the number of bits to check
                var bitsToCheck = subnetMask;
                var bytesToCheck = bitsToCheck / 8;
                var remainingBits = bitsToCheck % 8;
                
                // Check full bytes
                for (int i = 0; i < bytesToCheck; i++)
                {
                    if (ipBytes[i] != rangeBytes[i]) return false;
                }
                
                // Check remaining bits if any
                if (remainingBits > 0 && bytesToCheck < ipBytes.Length)
                {
                    var mask = (byte)(0xFF << (8 - remainingBits));
                    if ((ipBytes[bytesToCheck] & mask) != (rangeBytes[bytesToCheck] & mask))
                        return false;
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cleans up old inactive sessions
        /// </summary>
        public async Task CleanupOldSessionsAsync(TimeSpan maxAge)
        {
            var cutoffDate = DateTime.UtcNow - maxAge;
            
            var oldSessions = await _context.UserSessions
                .Where(s => s.LastSeenUtc < cutoffDate)
                .ToListAsync();
            
            foreach (var session in oldSessions)
            {
                session.IsActive = false;
            }
            
            await _context.SaveChangesAsync();
        }
    }
}
