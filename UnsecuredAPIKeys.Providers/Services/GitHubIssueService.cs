using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.Providers.Services;

public class GitHubIssueService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubIssueService>? _logger;
    private readonly string? _githubToken;

    public GitHubIssueService(HttpClient httpClient, ILogger<GitHubIssueService>? logger = null, string? githubToken = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _githubToken = githubToken;

        // Set up HTTP client with GitHub API headers
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UnsecuredAPIKeys-Bot", "1.0"));

        if (!string.IsNullOrEmpty(_githubToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", _githubToken);
        }
    }

    public async Task<IssueVerificationResult> CheckForIssueAsync(string repoUrl, string apiKeySnippet, string apiType)
    {
        try
        {
            // Extract owner and repo from URL
            var (owner, repo) = ExtractOwnerAndRepo(repoUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return new IssueVerificationResult
                {
                    Status = IssueVerificationStatus.VerificationError,
                    ErrorMessage = "Invalid repository URL format"
                };
            }

            // Search for issues related to this API key exposure
            var searchResults = await SearchIssuesAsync(owner, repo, apiType, apiKeySnippet);
            
            if (searchResults?.Items == null || !searchResults.Items.Any())
            {
                return new IssueVerificationResult
                {
                    Status = IssueVerificationStatus.NotFound
                };
            }

            // Find the most relevant issue (most recent one)
            var mostRecentIssue = searchResults.Items
                .OrderByDescending(i => i.CreatedAt)
                .First();

            return new IssueVerificationResult
            {
                Status = mostRecentIssue.State == "open" ? IssueVerificationStatus.Open : IssueVerificationStatus.Closed,
                IssueNumber = mostRecentIssue.Number,
                IssueTitle = mostRecentIssue.Title,
                IssueUrl = mostRecentIssue.HtmlUrl,
                CreatedAt = mostRecentIssue.CreatedAt,
                ClosedAt = mostRecentIssue.ClosedAt,
                GitHubUsername = mostRecentIssue.User?.Login,
                GitHubAvatarUrl = mostRecentIssue.User?.AvatarUrl,
                GitHubUserId = mostRecentIssue.User?.Id,
                GitHubDisplayName = mostRecentIssue.User?.Name
            };
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP error while checking for issues in {RepoUrl}", repoUrl);
            return new IssueVerificationResult
            {
                Status = IssueVerificationStatus.VerificationError,
                ErrorMessage = $"HTTP error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error while checking for issues in {RepoUrl}", repoUrl);
            return new IssueVerificationResult
            {
                Status = IssueVerificationStatus.VerificationError,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<GitHubSearchResult?> SearchIssuesAsync(string owner, string repo, string apiType, string apiKeySnippet)
    {
        try
        {
            // Create search query to find issues related to API key exposure
            var searchQueries = new[]
            {
                $"repo:{owner}/{repo} \"exposed\" \"{apiType}\" in:title,body",
                $"repo:{owner}/{repo} \"{apiType} key\" \"exposed\" in:title,body",
                $"repo:{owner}/{repo} \"API key\" \"exposed\" in:title,body",
                $"repo:{owner}/{repo} \"{apiKeySnippet}\" in:title,body" // Search for the specific key snippet
            };

            foreach (var query in searchQueries)
            {
                var encodedQuery = Uri.EscapeDataString(query);
                var url = $"https://api.github.com/search/issues?q={encodedQuery}&sort=created&order=desc&per_page=10";

                _logger?.LogDebug("Searching GitHub issues with query: {Query}", query);

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<GitHubSearchResult>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (searchResult?.TotalCount > 0)
                    {
                        return searchResult;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger?.LogWarning("GitHub API rate limit exceeded or access forbidden");
                    break; // Don't continue if we hit rate limits
                }

                // Small delay between queries to be respectful to the API
                await Task.Delay(100);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching GitHub issues for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    private static (string owner, string repo) ExtractOwnerAndRepo(string repoUrl)
    {
        try
        {
            var uri = new Uri(repoUrl);
            var pathSegments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (pathSegments.Length >= 2)
            {
                return (pathSegments[0], pathSegments[1]);
            }
        }
        catch (Exception)
        {
            // Invalid URL format
        }

        return (string.Empty, string.Empty);
    }
}

public class IssueVerificationResult
{
    public IssueVerificationStatus Status { get; set; }
    public int? IssueNumber { get; set; }
    public string? IssueTitle { get; set; }
    public string? IssueUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ErrorMessage { get; set; }
    
    // GitHub user information
    public string? GitHubUsername { get; set; }
    public string? GitHubAvatarUrl { get; set; }
    public long? GitHubUserId { get; set; }
    public string? GitHubDisplayName { get; set; }
}

public class GitHubSearchResult
{
    public int TotalCount { get; set; }
    public List<GitHubIssue> Items { get; set; } = new();
}

public class GitHubIssue
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public GitHubUser? User { get; set; }
}

public class GitHubUser
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string? Name { get; set; }
}