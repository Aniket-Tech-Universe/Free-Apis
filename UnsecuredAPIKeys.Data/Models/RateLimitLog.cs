using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models
{
    public class RateLimitLog
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(45)]
        public string IpAddress { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(256)]
        public string Endpoint { get; set; } = string.Empty;
        
        [Required]
        public DateTime RequestTimeUtc { get; set; }
    }
}
