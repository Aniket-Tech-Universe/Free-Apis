using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UnsecuredAPIKeys.Data.Models
{
    public class PatternEffectiveness
    {
        [Key]
        public long Id { get; set; }
        
        [Required]
        public string Pattern { get; set; } = string.Empty;
        
        public string ProviderName { get; set; } = string.Empty;
        
        public int TotalMatches { get; set; }
        public int ValidKeys { get; set; }
        public int InvalidKeys { get; set; }
        
        [NotMapped] // Tell EF Core to ignore this computed property
        public double SuccessRate => TotalMatches > 0 ? ValidKeys / (double)TotalMatches : 0;
        
        public DateTime FirstSeen { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Track which file types this pattern works best in
        public string? MostSuccessfulFileTypes { get; set; }
    }
}
