using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    /// <summary>
    /// Tracks user sessions and IP addresses for security and moderation purposes
    /// </summary>
    public class UserSession
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the Discord user (nullable for anonymous users)
        /// </summary>
        public int? DiscordUserId { get; set; }
        public virtual DiscordUser? DiscordUser { get; set; }
        
        /// <summary>
        /// IP address of the user
        /// </summary>
        [Required]
        [MaxLength(45)] // IPv6 can be up to 45 characters
        public string IpAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// User agent string
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// When this session was first created
        /// </summary>
        [Required]
        public DateTime FirstSeenUtc { get; set; }
        
        /// <summary>
        /// When this session was last active
        /// </summary>
        [Required]
        public DateTime LastSeenUtc { get; set; }
        
        /// <summary>
        /// Number of requests made in this session
        /// </summary>
        public int RequestCount { get; set; }
        
        /// <summary>
        /// Country code based on IP geolocation (optional)
        /// </summary>
        [MaxLength(2)]
        public string? CountryCode { get; set; }
        
        /// <summary>
        /// City based on IP geolocation (optional)
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }
        
        /// <summary>
        /// Whether this session is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Session identifier (for tracking across requests)
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }
    }
}
