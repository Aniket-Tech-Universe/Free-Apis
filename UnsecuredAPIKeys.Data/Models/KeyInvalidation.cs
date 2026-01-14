using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class KeyInvalidation
    {
        [Key]
        public long Id { get; set; }
        
        public long ApiKeyId { get; set; }
        public virtual APIKey ApiKey { get; set; } = null!;
        
        public DateTime InvalidatedAt { get; set; }
        public string? InvalidationReason { get; set; }
        public bool WasValid { get; set; }
        public int DaysActive { get; set; }
        
        // Track if key was fixed after being reported
        public bool ConfirmedFixed { get; set; }
        public DateTime? FixedAt { get; set; }
        
        // Additional tracking
        public string? PreviousStatus { get; set; }
        public string? HttpStatusCode { get; set; }
    }
}
