using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models;

public class SnitchLeaderboard
{
    [Key]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string UserIdentifier { get; set; } = string.Empty; // IP address or hashed identifier

    [MaxLength(100)]
    public string? DisplayName { get; set; } // Optional display name

    public int TotalIssuesSubmitted { get; set; } = 0;

    public int OpenIssuesSubmitted { get; set; } = 0;

    public int ClosedIssuesSubmitted { get; set; } = 0;

    public DateTime FirstSubmissionAt { get; set; }

    public DateTime LastSubmissionAt { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    // Additional stats for fun
    public int TotalRepositoriesAffected { get; set; } = 0;

    [MaxLength(20)]
    public string? FavoriteApiType { get; set; } // Most reported API type

    public int ConsecutiveDaysActive { get; set; } = 0;

    public double SnitchScore { get; set; } = 0.0; // Calculated score for ranking
}