using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class DonationSupporter
    {
        [Key]
        public long Id { get; set; }
        
        /// <summary>
        /// Reference to the DonationTracking record
        /// </summary>
        public long DonationTrackingId { get; set; }
        public virtual DonationTracking DonationTracking { get; set; } = null!;
        
        /// <summary>
        /// PayPal transaction ID for matching with IPN data
        /// </summary>
        public string? PayPalTransactionId { get; set; }
        
        /// <summary>
        /// Display name for the supporters page
        /// </summary>
        [MaxLength(100)]
        public string DisplayName { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional website URL
        /// </summary>
        [MaxLength(500)]
        public string? WebsiteUrl { get; set; }
        
        /// <summary>
        /// Discord user ID if logged in
        /// </summary>
        public long? DiscordUserId { get; set; }
        public virtual DiscordUser? DiscordUser { get; set; }
        
        /// <summary>
        /// Discord username if not logged in
        /// </summary>
        [MaxLength(100)]
        public string? DiscordUsername { get; set; }
        
        /// <summary>
        /// Whether to show on the public supporters page
        /// </summary>
        public bool ShowOnSupportersPage { get; set; } = true;
        
        /// <summary>
        /// When the supporter info was submitted
        /// </summary>
        public DateTime SubmittedAt { get; set; }
        
        /// <summary>
        /// IP address for tracking
        /// </summary>
        public string? UserIP { get; set; }
        
        /// <summary>
        /// Additional notes or preferences
        /// </summary>
        public string? Notes { get; set; }
    }
}
