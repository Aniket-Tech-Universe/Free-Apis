using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NGitLab;
using NGitLab.Models;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.Providers._Interfaces;
// Added NGitLab using
// Ensure HttpClient using is present
// Ensure Headers using is present
using ProviderSearchQuery = UnsecuredAPIKeys.Data.Models.SearchQuery;


namespace UnsecuredAPIKeys.Providers.Search_Providers
{
    // Helper classes for deserializing GitLab search results
    internal class GitLabBlobSearchResult
    {
        [JsonPropertyName("project_id")]
        public long ProjectId { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty; // The code snippet containing the match

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty; // Often same as Path for blobs

        [JsonPropertyName("ref")]
        public string Ref { get; set; } = string.Empty; // Branch or commit SHA

        [JsonPropertyName("startline")]
        public int StartLine { get; set; }

        // Other potential fields: basename, users (if searching commits)
    }

    /// <summary>
    /// Implements the ISearchProvider interface for searching code blobs on GitLab.
    /// </summary>
    public class GitLabSearchProvider(ILogger<GitLabSearchProvider> logger, IHttpClientFactory httpClientFactory)
        : ISearchProvider
    {
        private readonly ILogger<GitLabSearchProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly HttpClient _httpClient = httpClientFactory?.CreateClient("GitLabProviderClient") ?? throw new ArgumentNullException(nameof(httpClientFactory)); // Field to store HttpClient
        private const string DefaultGitLabUrl = "https://gitlab.com";

        // Constructor accepting IHttpClientFactory and ILogger
        // Made logger non-optional as it's generally needed
        // Base address and default headers should ideally be configured when registering the typed client

        /// <inheritdoc />
        public string ProviderName => "GitLab";

        /// <inheritdoc />
        // Use the fully qualified name or alias for our SearchQuery model
        public async Task<IEnumerable<RepoReference>> SearchAsync(ProviderSearchQuery query, SearchProviderToken? token)
        {
            if (token == null || string.IsNullOrWhiteSpace(token.Token))
            {
                _logger?.LogError("GitLab token is missing or invalid.");
                throw new ArgumentNullException(nameof(token), "A valid GitLab token is required.");
            }

            if (query == null || string.IsNullOrWhiteSpace(query.Query))
            {
                _logger?.LogError("Search query is missing or invalid.");
                throw new ArgumentNullException(nameof(query), "A valid search query is required.");
            }

            // Determine GitLab instance URL (default to gitlab.com if not specified in token or query)
            // TODO: Add a mechanism to specify the GitLab instance URL if needed (e.g., in SearchProviderToken or SearchQuery)
            var gitLabUrl = DefaultGitLabUrl;
            _logger?.LogInformation("Using GitLab instance URL: {GitLabUrl}", gitLabUrl);

            var client = new GitLabClient(gitLabUrl, token.Token);
            var results = new List<RepoReference>();
            int page = 1; // GitLab API is 1-based index for pages
            const int perPage = 100; // Max items per page for GitLab search? Check docs (often 20 or 100)

            try
            {
                _logger?.LogInformation("Starting GitLab blob search for query: {Query}", query.Query);

                while (true)
                {
                    // Use SearchBlob as the expected return type
                    // Use lower-level GET request as library methods are problematic
                    List<GitLabBlobSearchResult>? searchResultPage = null;
                    HttpResponseMessage? response = null; // Keep response variable
                    try
                    {
                        // Construct the API URL manually
                        string searchUrl = $"{gitLabUrl}/api/v4/search?scope=blobs&search={Uri.EscapeDataString(query.Query)}&page={page}&per_page={perPage}";
                        _logger?.LogDebug("Requesting GitLab search URL: {Url}", searchUrl);

                        // Use the injected HttpClient
                        using var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        request.Headers.Authorization = null; // Clear any default auth if client is shared
                        request.Headers.Add("PRIVATE-TOKEN", token.Token); // Add GitLab token header

                        response = await _httpClient.SendAsync(request);

                        // Check for rate limiting status code first
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger?.LogWarning("GitLab API rate limit exceeded (429).");
                            var retryAfter = response.Headers.RetryAfter?.Delta;
                            if (retryAfter.HasValue && retryAfter.Value > TimeSpan.Zero)
                            {
                                _logger?.LogWarning("Waiting {RetryAfterTotalSeconds:F0}s based on Retry-After header.", retryAfter.Value.TotalSeconds);
                                await Task.Delay(retryAfter.Value);
                            }
                            else
                            {
                                _logger?.LogWarning("No Retry-After header found or value invalid. Waiting 60 seconds.");
                                await Task.Delay(TimeSpan.FromSeconds(60));
                            }
                            response.Dispose(); // Dispose the response before retrying
                            continue; // Retry the same page
                        }

                       response.EnsureSuccessStatusCode(); // Throw for other non-success codes

                       // Deserialize the response
                        var jsonStream = await response.Content.ReadAsStreamAsync();
                        searchResultPage = await JsonSerializer.DeserializeAsync<List<GitLabBlobSearchResult>>(jsonStream);

                        // TODO: Extract rate limit headers from response.Headers if needed
                    }
                    // Catch specific exceptions like HttpRequestException or JsonException
                    catch (HttpRequestException httpEx)
                    {
                        _logger?.LogError(httpEx, "HTTP error during GitLab search request on page {Page}. Status: {StatusCode}", page, httpEx.StatusCode);
                        break; // Stop searching on HTTP error
                    }
                    catch (JsonException jsonEx)
                    {
                         _logger?.LogError(jsonEx, "Error deserializing GitLab search response on page {Page}.", page);
                         break; // Stop searching on JSON error
                    }
                    catch (Exception ex) // Catch any other unexpected errors
                    {
                        _logger?.LogError(ex, "An unexpected error occurred during GitLab search processing on page {Page} for query: {Query}", page, query.Query);
                        break; // Stop searching on unexpected error
                    }

                    // Use the deserialized list
                    var pageItems = searchResultPage ?? [];

                    if (pageItems.Count == 0)
                    {
                        _logger?.LogInformation("No more results found for query '{Query}' on page {Page}.", query.Query, page);
                        break; // No more results
                    }

                    _logger?.LogDebug("Found {Count} results on page {Page} for query '{Query}'.", pageItems.Count, page, query.Query);

                    foreach (var item in pageItems)
                    {
                        // Use properties from GitLabBlobSearchResult
                        Project? project = null;
                        try
                        {
                            // Use ProjectId from the deserialized item
                            project = await client.Projects.GetByIdAsync(item.ProjectId, new SingleProjectQuery());
                        }
                         catch (NGitLab.GitLabException glEx) when (glEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger?.LogWarning("Project ID {ProjectId} not found. Skipping item.", item.ProjectId);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to fetch project details for Project ID {ProjectId}. Skipping item.", item.ProjectId);
                            continue;
                        }

                        if (project == null) continue;

                        // Construct URLs using properties from deserialized item and Project
                        string filePath = item.Path;
                        string branchOrRef = item.Ref ?? project.DefaultBranch; // Ref is string here

                        // File URL (HTML view)
                        string fileHtmlUrl = $"{project.WebUrl}/-/blob/{branchOrRef}/{filePath}";
                        // API Content URL
                        string apiContentUrl = $"{gitLabUrl}/api/v4/projects/{project.Id}/repository/files/{Uri.EscapeDataString(filePath)}?ref={branchOrRef}";


                        results.Add(new RepoReference
                        {
                            SearchQueryId = query.Id,
                            Provider = ProviderName,
                            RepoOwner = project.Namespace?.FullPath,
                            RepoName = project.Name,
                            RepoURL = project.WebUrl,
                            RepoId = project.Id,
                            RepoDescription = project.Description,
                            FilePath = filePath,
                            FileName = item.Filename, // Use filename from result
                            FileURL = fileHtmlUrl,
                            ApiContentUrl = apiContentUrl,
                            Branch = branchOrRef,
                            FileSHA = item.Ref, // Use the ref string as SHA context
                            FoundUTC = DateTime.UtcNow,
                            // CodeContext could potentially use item.Data, but it's just a snippet
                            // LineNumber = item.StartLine // Store the start line from the search result
                        });
                    }

                    // Check if this was the last page
                    if (pageItems.Count < perPage)
                    {
                        _logger?.LogInformation("Finished GitLab search for query '{Query}'. Reached last page {Page}.", query.Query, page);
                        break;
                    }

                    page++; // Move to the next page

                    // Add a small delay to be polite to the API, especially if fetching project details per item
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex) // Catch errors initializing client or other unexpected issues
            {
                _logger?.LogError(ex, "An unexpected error occurred during GitLab search setup or outer loop for query: {Query}", query.Query);
            }

            _logger?.LogInformation("Completed GitLab search for query '{Query}'. Found {Count} potential references.", query.Query, results.Count);
            return results;
        }
    }
}
