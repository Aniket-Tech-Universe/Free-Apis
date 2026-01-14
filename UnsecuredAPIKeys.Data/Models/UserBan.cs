using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    /// <summary>
    /// Tracks banned users and IP addresses
    /// </summary>
    public class UserBan
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Type of ban (IP, DiscordUser, or Both)
        /// </summary>
        [Required]
        public BanType BanType { get; set; }
        
        /// <summary>
        /// Discord user ID if banning a specific user
        /// </summary>
        public int? DiscordUserId { get; set; }
        public virtual DiscordUser? DiscordUser { get; set; }
        
        /// <summary>
        /// IP address to ban (can be specific IP or CIDR range)
        /// </summary>
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        /// <summary>
        /// CIDR subnet mask for IP range bans (e.g., /24, /16)
        /// </summary>
        public int? SubnetMask { get; set; }
        
        /// <summary>
        /// Reason for the ban
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
        
        /// <summary>
        /// Admin who issued the ban
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string BannedBy { get; set; } = string.Empty;
        
        /// <summary>
        /// When the ban was issued
        /// </summary>
        [Required]
        public DateTime BannedAtUtc { get; set; }
        
        /// <summary>
        /// When the ban expires (null for permanent bans)
        /// </summary>
        public DateTime? ExpiresAtUtc { get; set; }
        
        /// <summary>
        /// Whether the ban is currently active
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Additional notes about the ban
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
        
        /// <summary>
        /// When the ban was last updated
        /// </summary>
        public DateTime? UpdatedAtUtc { get; set; }
        
        /// <summary>
        /// Who last updated the ban
        /// </summary>
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
    }
    
    public enum BanType
    {
        /// <summary>
        /// Ban specific IP address
        /// </summary>
        IpAddress = 1,
        
        /// <summary>
        /// Ban Discord user (across all IPs)
        /// </summary>
        DiscordUser = 2,
        
        /// <summary>
        /// Ban both the Discord user and their known IP addresses
        /// </summary>
        Both = 3,
        
        /// <summary>
        /// Ban an IP range using CIDR notation
        /// </summary>
        IpRange = 4
    }
}
