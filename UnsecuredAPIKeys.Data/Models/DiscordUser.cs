using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class DiscordUser
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string DiscordId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [MaxLength(10)]
        public string? Discriminator { get; set; }
        
        [MaxLength(200)]
        public string? Avatar { get; set; }
        
        [MaxLength(100)]
        public string? Email { get; set; }
        
        [Required]
        public bool IsServerMember { get; set; }
        
        [Required]
        public DateTime FirstLoginUtc { get; set; }
        
        [Required]
        public DateTime LastLoginUtc { get; set; }
        
        [Required]
        public DateTime LastMembershipCheckUtc { get; set; }
        
        /// <summary>
        /// Access token for Discord API calls (encrypted)
        /// </summary>
        [MaxLength(500)]
        public string? AccessToken { get; set; }
        
        /// <summary>
        /// Refresh token for Discord OAuth (encrypted)
        /// </summary>
        [MaxLength(500)]
        public string? RefreshToken { get; set; }
        
        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime? TokenExpiresAt { get; set; }
        
        /// <summary>
        /// Rate limit override for Discord members
        /// </summary>
        public int? RateLimitOverride { get; set; }
        
        /// <summary>
        /// Comma-separated list of role IDs the user has in the server
        /// </summary>
        [MaxLength(1000)]
        public string? ServerRoles { get; set; }
        
        /// <summary>
        /// When the user's roles were last checked
        /// </summary>
        public DateTime? LastRoleCheckUtc { get; set; }
        
        /// <summary>
        /// Highest tier role for rate limiting
        /// </summary>
        [MaxLength(50)]
        public string? HighestTier { get; set; }
        
        /// <summary>
        /// When the user was last seen active
        /// </summary>
        public DateTime? LastSeenUtc { get; set; }
        
        /// <summary>
        /// Last known IP address of the user
        /// </summary>
        [MaxLength(45)]
        public string? LastKnownIpAddress { get; set; }
        
        /// <summary>
        /// When the IP address was last updated
        /// </summary>
        public DateTime? IpLastUpdatedUtc { get; set; }
        
        /// <summary>
        /// Navigation property for user sessions
        /// </summary>
        public virtual ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
        
        /// <summary>
        /// Navigation property for user bans
        /// </summary>
        public virtual ICollection<UserBan> Bans { get; set; } = new List<UserBan>();
    }
}
