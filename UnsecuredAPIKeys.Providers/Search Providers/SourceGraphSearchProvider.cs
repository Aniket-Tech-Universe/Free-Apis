using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.Providers._Interfaces;

namespace UnsecuredAPIKeys.Providers.Search_Providers
{
    public class SourceGraphSearchProvider(DBContext dbContext, ILogger<SourceGraphSearchProvider>? logger = null) : ISearchProvider
    {
        private readonly HttpClient _httpClient = new();
        private const string API_ENDPOINT = "https://sourcegraph.com/.api/graphql";
        private const int MAX_RETRIES = 5;

        public string ProviderName => "SourceGraph";

        public async Task<IEnumerable<RepoReference>> SearchAsync(SearchQuery query, SearchProviderToken? token)
        {
            ValidateInputs(query, token); // Ensures token and token.Token are not null/whitespace
            if (token == null || token.Token == null) {
                // This case should ideally be prevented by ValidateInputs, 
                // but as a safeguard for the compiler:
                throw new InvalidOperationException("Token should not be null here due to ValidateInputs.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token.Token);

            var results = new List<RepoReference>();

            try
            {
                logger?.LogInformation("Starting SourceGraph search for query: {Query}", query.Query);
                var graphQLQuery = CreateGraphQLQuery(query);
                var response = await ExecuteRequestWithRetry(graphQLQuery);

                if (response?.IsSuccessStatusCode != true)
                {
                    logger?.LogError("Failed to get successful response from SourceGraph");
                    return results;
                }

                var searchResult = await ProcessResponse(response, query);
                results.AddRange(searchResult);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during SourceGraph search for query: {Query}", query.Query);
            }

            logger?.LogInformation("Completed SourceGraph search. Found {Count} references for '{Query}'", results.Count, query.Query);
            return results;
        }

        private void ValidateInputs(SearchQuery query, SearchProviderToken? token)
        {
            if (string.IsNullOrWhiteSpace(token?.Token))
                throw new ArgumentException("Valid SourceGraph token required", nameof(token));

            if (string.IsNullOrWhiteSpace(query?.Query))
                throw new ArgumentException("Valid search query required", nameof(query));
        }

        private object CreateGraphQLQuery(SearchQuery query)
        {
            var sanitizedQuery = query.Query.Replace("\"", " ");
            return new
            {
                query = $@"query {{
                    search(query: ""context:global {sanitizedQuery} count:all"", version: V2) {{
                        results {{
                            matchCount
                            limitHit
                            results {{
                                ... on FileMatch {{
                                    file {{ path url }}
                                    repository {{ name url description defaultBranch {{ name }} }}
                                }}
                            }}
                        }}
                    }}
                }}"
            };
        }

        private async Task<HttpResponseMessage?> ExecuteRequestWithRetry(object graphQLQuery)
        {
            HttpResponseMessage? response = null;
            for (int retry = 0; retry < MAX_RETRIES; retry++)
            {
                response = await _httpClient.PostAsJsonAsync(API_ENDPOINT, graphQLQuery);

                if (response != null && (int)response.StatusCode != 429) // Added null check for response
                    return response;
                else if (response == null)
                {
                    // If response is null, something went wrong with PostAsJsonAsync, break retry.
                    logger?.LogError("HttpClient.PostAsJsonAsync returned null unexpectedly.");
                    return null; 
                }


                var retryAfter = response.Headers.RetryAfter?.Delta;
                if (retryAfter.HasValue && retryAfter.Value > TimeSpan.Zero)
                {
                    logger?.LogWarning("Rate limited. Waiting {RetryAfterTotalSeconds:F0}s based on Retry-After header (retry {Retry}/{MaxRetries})", retryAfter.Value.TotalSeconds, retry + 1, MAX_RETRIES);
                    await Task.Delay(retryAfter.Value);
                }
                else
                {
                    var delay = (int)Math.Pow(2, retry + 1);
                    logger?.LogWarning("Rate limited. No Retry-After header found or value invalid. Waiting {Delay}s (retry {Retry}/{MaxRetries})", delay, retry + 1, MAX_RETRIES);
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                }
                response.Dispose(); // Dispose the response before retrying
            }
            return response; // response could still be the one from the last attempt (e.g. a 429) or null if MAX_RETRIES was 0
        }

       private async Task<List<RepoReference>> ProcessResponse(HttpResponseMessage? response, SearchQuery query)
        {
            var results = new List<RepoReference>();
            if (response == null)
            {
                logger?.LogError("HttpResponseMessage was null in ProcessResponse for query: {Query}", query.Query);
                return results;
            }
            var content = await response.Content.ReadFromJsonAsync<SourceGraphResponse>();
            var searchData = content?.Data?.Search?.Results;

            if (searchData?.Results == null || searchData.Results.Count == 0)
            {
                logger?.LogDebug("No results found for query '{Query}'", query.Query);
                return results;
            }

            await UpdateQueryStats(query, searchData.MatchCount);

            foreach (var item in searchData.Results)
            {
                var repoRef = CreateRepoReference(query, item);
                if (repoRef != null) results.Add(repoRef);
            }

            if (searchData.LimitHit)
                logger?.LogWarning("Result limit hit for query '{Query}'", query.Query);

            return results;
        }

        private async Task UpdateQueryStats(SearchQuery query, int matchCount)
        {
            try
            {
                query.SearchResultsCount = matchCount;
                dbContext.SearchQueries.Update(query);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to update query stats for query ID {QueryId}", query.Id);
            }
        }

        private RepoReference? CreateRepoReference(SearchQuery query, FileMatchResult item)
        {
            try
            {
                var repo = item.Repository;
                var file = item.File;

                if (repo?.Url == null || file?.Url == null)
                {
                    logger?.LogDebug("Skipping invalid result item");
                    return null;
                }

                var repoNameParts = repo.Name?.Split('/') ?? [];
                return new RepoReference
                {
                    SearchQueryId = query.Id,
                    Provider = ProviderName,
                    RepoOwner = repoNameParts.Length > 1 ? repoNameParts[0] : null,
                    RepoName = repoNameParts.Length > 1 ? repoNameParts[1] : repo.Name,
                    FilePath = file.Path,
                    FileURL = file.Url,
                    ApiContentUrl = file.Url,
                    Branch = repo.DefaultBranch?.Name,
                    FoundUTC = DateTime.UtcNow,
                    RepoURL = repo.Url,
                    RepoDescription = repo.Description,
                    FileName = Path.GetFileName(file.Path)
                };
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error creating RepoReference");
                return null;
            }
        }
    }

    #region Response Classes
    internal class SourceGraphResponse
    {
        [JsonPropertyName("data")] public SearchData Data { get; set; } = new();
    }

    internal class SearchData
    {
        [JsonPropertyName("search")] public SearchResults Search { get; set; } = new();
    }

    internal class SearchResults
    {
        [JsonPropertyName("results")] public ResultsContainer Results { get; set; } = new();
    }

    internal class ResultsContainer
    {
        [JsonPropertyName("results")] public List<FileMatchResult> Results { get; set; } = [];
        [JsonPropertyName("matchCount")] public int MatchCount { get; set; }
        [JsonPropertyName("limitHit")] public bool LimitHit { get; set; }
    }

    internal class FileMatchResult
    {
        [JsonPropertyName("repository")] public Repository Repository { get; set; } = new();
        [JsonPropertyName("file")] public File File { get; set; } = new();
    }

    internal class Repository
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
        [JsonPropertyName("defaultBranch")] public Branch DefaultBranch { get; set; } = new();
    }

    internal class Branch
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    }

    internal class File
    {
        [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
    }
    #endregion
}
