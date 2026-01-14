using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class KeyRotation
    {
        [Key]
        public long Id { get; set; }
        
        public long OldKeyId { get; set; }
        public virtual APIKey OldKey { get; set; } = null!;
        
        public long NewKeyId { get; set; }
        public virtual APIKey NewKey { get; set; } = null!;
        
        public DateTime RotatedAt { get; set; }
        public string RepoUrl { get; set; } = string.Empty;
        
        // Track how long the old key was active
        public int OldKeyDaysActive { get; set; }
    }
}
