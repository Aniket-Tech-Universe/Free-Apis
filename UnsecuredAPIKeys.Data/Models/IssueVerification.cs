using System.ComponentModel.DataAnnotations;
using UnsecuredAPIKeys.Data.Common;

namespace UnsecuredAPIKeys.Data.Models;

public class IssueVerification
{
    [Key]
    public long Id { get; set; }

    [Required]
    public int IssueSubmissionTrackingId { get; set; }

    [Required]
    [MaxLength(500)]
    public string RepoUrl { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? IssueTitle { get; set; }

    public long? GitHubIssueNumber { get; set; }

    [MaxLength(500)]
    public string? GitHubIssueUrl { get; set; }

    public IssueVerificationStatus Status { get; set; } = IssueVerificationStatus.NotFound;

    public DateTime? IssueCreatedAt { get; set; }
    public DateTime? IssueClosedAt { get; set; }

    public DateTime FirstCheckedAt { get; set; }
    public DateTime LastCheckedAt { get; set; }

    [MaxLength(50)]
    public string? SubmitterIP { get; set; }

    // GitHub issue creator information
    [MaxLength(100)]
    public string? GitHubUsername { get; set; }

    [MaxLength(500)]
    public string? GitHubAvatarUrl { get; set; }

    public long? GitHubUserId { get; set; }

    [MaxLength(200)]
    public string? GitHubDisplayName { get; set; }

    // Navigation property
    public IssueSubmissionTracking? IssueSubmissionTracking { get; set; }
}
