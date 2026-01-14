using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class DonationTracking
    {
        [Key]
        public long Id { get; set; }
        
        /// <summary>
        /// Timestamp when the PayPal button was clicked
        /// </summary>
        public DateTime ClickedAt { get; set; }
        
        /// <summary>
        /// IP address of the user who clicked (for deduplication)
        /// </summary>
        public string UserIP { get; set; } = string.Empty;
        
        /// <summary>
        /// Location where the button was clicked (e.g., "hero_section", "stats_section")
        /// </summary>
        public string ClickLocation { get; set; } = string.Empty;
        
        /// <summary>
        /// User agent for analytics
        /// </summary>
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// Session ID if available
        /// </summary>
        public string? SessionId { get; set; }
        
        /// <summary>
        /// Whether this click resulted in a confirmed donation
        /// </summary>
        public bool ConfirmedDonation { get; set; } = false;
        
        /// <summary>
        /// Amount donated (if confirmed)
        /// </summary>
        public decimal? DonationAmount { get; set; }
        
        /// <summary>
        /// PayPal transaction ID (if available)
        /// </summary>
        public string? PayPalTransactionId { get; set; }
        
        /// <summary>
        /// When the donation was confirmed
        /// </summary>
        public DateTime? DonationConfirmedAt { get; set; }
        
        /// <summary>
        /// Additional notes or metadata
        /// </summary>
        public string? Notes { get; set; }
        
        /// <summary>
        /// Donor's email address from PayPal
        /// </summary>
        public string? DonorEmail { get; set; }
        
        /// <summary>
        /// Donor's first name from PayPal
        /// </summary>
        public string? DonorFirstName { get; set; }
        
        /// <summary>
        /// Donor's last name from PayPal
        /// </summary>
        public string? DonorLastName { get; set; }
        
        /// <summary>
        /// PayPal Payer ID
        /// </summary>
        public string? PayPalPayerId { get; set; }
        
        /// <summary>
        /// PayPal payment status (Completed, Pending, etc.)
        /// </summary>
        public string? PaymentStatus { get; set; }
        
        /// <summary>
        /// Whether IPN was verified with PayPal
        /// </summary>
        public bool? IPNVerified { get; set; }
        
        /// <summary>
        /// Raw IPN message for debugging
        /// </summary>
        public string? IPNRawMessage { get; set; }
    }
}
