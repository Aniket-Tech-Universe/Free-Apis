using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.Providers.Services;

public class SnitchLeaderboardService(DBContext dbContext, ILogger<SnitchLeaderboardService>? logger = null)
{
    public async Task ProcessIssueVerificationAsync(IssueVerification issueVerification)
    {
        try
        {
            if (string.IsNullOrEmpty(issueVerification.SubmitterIP))
            {
                logger?.LogWarning("No submitter IP found for issue verification {IssueVerificationId}", issueVerification.Id);
                return;
            }

            // Get or create leaderboard entry for this user
            var leaderboardEntry = await dbContext.SnitchLeaderboards
                .FirstOrDefaultAsync(s => s.UserIdentifier == issueVerification.SubmitterIP);

            if (leaderboardEntry == null)
            {
                leaderboardEntry = new SnitchLeaderboard
                {
                    UserIdentifier = issueVerification.SubmitterIP,
                    FirstSubmissionAt = DateTime.UtcNow,
                    LastSubmissionAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };
                dbContext.SnitchLeaderboards.Add(leaderboardEntry);
            }

            // Update statistics
            leaderboardEntry.TotalIssuesSubmitted++;
            
            if (issueVerification.Status == IssueVerificationStatus.Open)
            {
                leaderboardEntry.OpenIssuesSubmitted++;
            }
            else if (issueVerification.Status == IssueVerificationStatus.Closed)
            {
                leaderboardEntry.ClosedIssuesSubmitted++;
            }

            leaderboardEntry.LastSubmissionAt = DateTime.UtcNow;
            leaderboardEntry.LastUpdatedAt = DateTime.UtcNow;

            // Update repository count
            await UpdateRepositoryCountAsync(leaderboardEntry.UserIdentifier);

            // Update favorite API type
            await UpdateFavoriteApiTypeAsync(leaderboardEntry.UserIdentifier);

            // Calculate and update snitch score
            leaderboardEntry.SnitchScore = await CalculateSnitchScoreAsync(leaderboardEntry.UserIdentifier);

            await dbContext.SaveChangesAsync();

            logger?.LogInformation("Updated leaderboard for user {UserIdentifier}. Total issues: {TotalIssues}, Score: {Score}", 
                leaderboardEntry.UserIdentifier, leaderboardEntry.TotalIssuesSubmitted, leaderboardEntry.SnitchScore);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error processing issue verification for leaderboard");
            throw;
        }
    }

    public async Task<List<SnitchLeaderboardEntry>> GetTopSnitchesAsync(int count = 10)
    {
        try
        {
            var topSnitches = await dbContext.SnitchLeaderboards
                .OrderByDescending(s => s.SnitchScore)
                .Take(count)
                .ToListAsync();

            var result = new List<SnitchLeaderboardEntry>();
            
            for (int i = 0; i < topSnitches.Count; i++)
            {
                var entry = topSnitches[i];
                
                // Get GitHub user information for this submitter
                var githubUsers = await dbContext.IssueVerifications
                    .Where(iv => iv.SubmitterIP == entry.UserIdentifier &&
                                !string.IsNullOrEmpty(iv.GitHubUsername))
                    .GroupBy(iv => new { iv.GitHubUsername, iv.GitHubAvatarUrl, iv.GitHubUserId, iv.GitHubDisplayName })
                    .Select(g => new GitHubUserInfo
                    {
                        Username = g.Key.GitHubUsername ?? "",
                        AvatarUrl = g.Key.GitHubAvatarUrl ?? "",
                        UserId = g.Key.GitHubUserId,
                        DisplayName = g.Key.GitHubDisplayName,
                        IssueCount = g.Count()
                    })
                    .OrderByDescending(u => u.IssueCount)
                    .ToListAsync();

                result.Add(new SnitchLeaderboardEntry
                {
                    Rank = i + 1,
                    UserIdentifier = entry.UserIdentifier,
                    DisplayName = entry.DisplayName ?? GenerateAnonymousDisplayName(entry.UserIdentifier),
                    TotalIssuesSubmitted = entry.TotalIssuesSubmitted,
                    OpenIssuesSubmitted = entry.OpenIssuesSubmitted,
                    ClosedIssuesSubmitted = entry.ClosedIssuesSubmitted,
                    TotalRepositoriesAffected = entry.TotalRepositoriesAffected,
                    FavoriteApiType = entry.FavoriteApiType,
                    SnitchScore = entry.SnitchScore,
                    FirstSubmissionAt = entry.FirstSubmissionAt,
                    LastSubmissionAt = entry.LastSubmissionAt,
                    ConsecutiveDaysActive = entry.ConsecutiveDaysActive,
                    GitHubUsers = githubUsers
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error retrieving top snitches from leaderboard");
            throw;
        }
    }

    private async Task UpdateRepositoryCountAsync(string userIdentifier)
    {
        try
        {
            var distinctRepos = await dbContext.IssueSubmissionTrackings
                .Where(i => i.UserIP == userIdentifier)
                .Select(i => i.RepoUrl)
                .Distinct()
                .CountAsync();

            var leaderboardEntry = await dbContext.SnitchLeaderboards
                .FirstOrDefaultAsync(s => s.UserIdentifier == userIdentifier);

            if (leaderboardEntry != null)
            {
                leaderboardEntry.TotalRepositoriesAffected = distinctRepos;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error updating repository count for user {UserIdentifier}", userIdentifier);
        }
    }

    private async Task UpdateFavoriteApiTypeAsync(string userIdentifier)
    {
        try
        {
            var favoriteApiType = await dbContext.IssueSubmissionTrackings
                .Where(i => i.UserIP == userIdentifier)
                .GroupBy(i => i.ApiType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            var leaderboardEntry = await dbContext.SnitchLeaderboards
                .FirstOrDefaultAsync(s => s.UserIdentifier == userIdentifier);

            if (leaderboardEntry != null)
            {
                leaderboardEntry.FavoriteApiType = favoriteApiType;
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error updating favorite API type for user {UserIdentifier}", userIdentifier);
        }
    }

    private async Task<double> CalculateSnitchScoreAsync(string userIdentifier)
    {
        try
        {
            var userStats = await dbContext.IssueVerifications
                .Where(iv => iv.SubmitterIP == userIdentifier)
                .GroupBy(iv => iv.SubmitterIP)
                .Select(g => new
                {
                    TotalIssues = g.Count(),
                    OpenIssues = g.Count(iv => iv.Status == IssueVerificationStatus.Open),
                    ClosedIssues = g.Count(iv => iv.Status == IssueVerificationStatus.Closed)
                })
                .FirstOrDefaultAsync();

            if (userStats == null)
            {
                return 0.0;
            }

            // Calculate score based on various factors
            double score = 0.0;

            // Base points for total issues
            score += userStats.TotalIssues * 10;

            // Bonus points for closed issues (indicates successful reporting)
            score += userStats.ClosedIssues * 15;

            // Bonus for open issues (recent activity)
            score += userStats.OpenIssues * 5;

            // Get repository diversity bonus
            var distinctRepos = await dbContext.IssueSubmissionTrackings
                .Where(i => i.UserIP == userIdentifier)
                .Select(i => i.RepoUrl)
                .Distinct()
                .CountAsync();

            score += distinctRepos * 20; // Bonus for affecting multiple repositories

            // Calculate time-based bonus (more recent activity gets higher score)
            var lastSubmission = await dbContext.IssueSubmissionTrackings
                .Where(i => i.UserIP == userIdentifier)
                .OrderByDescending(i => i.SubmittedAt)
                .Select(i => i.SubmittedAt)
                .FirstOrDefaultAsync();

            if (lastSubmission != default)
            {
                var daysSinceLastSubmission = (DateTime.UtcNow - lastSubmission).Days;
                
                if (daysSinceLastSubmission <= 7)
                {
                    score *= 1.5; // 50% bonus for activity in last week
                }
                else if (daysSinceLastSubmission <= 30)
                {
                    score *= 1.2; // 20% bonus for activity in last month
                }
            }

            return Math.Round(score, 2);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error calculating snitch score for user {UserIdentifier}", userIdentifier);
            return 0.0;
        }
    }

    private static string GenerateAnonymousDisplayName(string userIdentifier)
    {
        // Generate a fun anonymous name based on the IP hash
        var hash = userIdentifier.GetHashCode();
        var adjectives = new[] { "Anonymous", "Mysterious", "Stealthy", "Vigilant", "Alert", "Sharp", "Keen", "Clever", "Swift", "Silent" };
        var nouns = new[] { "Snitch", "Watcher", "Guardian", "Detective", "Hunter", "Scout", "Finder", "Seeker", "Tracker", "Spy" };
        
        var adjIndex = Math.Abs(hash) % adjectives.Length;
        var nounIndex = Math.Abs(hash / adjectives.Length) % nouns.Length;
        
        return $"{adjectives[adjIndex]} {nouns[nounIndex]} #{Math.Abs(hash) % 1000:D3}";
    }
}

public class SnitchLeaderboardEntry
{
    public int Rank { get; set; }
    public string UserIdentifier { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int TotalIssuesSubmitted { get; set; }
    public int OpenIssuesSubmitted { get; set; }
    public int ClosedIssuesSubmitted { get; set; }
    public int TotalRepositoriesAffected { get; set; }
    public string? FavoriteApiType { get; set; }
    public double SnitchScore { get; set; }
    public DateTime FirstSubmissionAt { get; set; }
    public DateTime LastSubmissionAt { get; set; }
    public int ConsecutiveDaysActive { get; set; }
    
    // GitHub user information from actual issue creators
    public List<GitHubUserInfo> GitHubUsers { get; set; } = new();
}

public class GitHubUserInfo
{
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public long? UserId { get; set; }
    public string? DisplayName { get; set; }
    public int IssueCount { get; set; } // Number of issues this GitHub user created for this submitter
}
