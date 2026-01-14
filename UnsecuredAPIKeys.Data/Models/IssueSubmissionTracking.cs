using System.ComponentModel.DataAnnotations;

namespace UnsecuredAPIKeys.Data.Models;

public class IssueSubmissionTracking
{
    [Key]
    public int Id { get; set; }

    public long ApiKeyId { get; set; }

    [Required]
    [MaxLength(100)]
    public string ApiType { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string RepoUrl { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }
    public string? UserIP { get; set; } //Who submitted the issue?
}