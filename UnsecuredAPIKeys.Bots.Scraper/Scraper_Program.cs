using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.Providers;
using UnsecuredAPIKeys.Providers._Interfaces;

namespace UnsecuredAPIKeys.Bots.Scraper
{
    internal static class Scraper_Program
    {
        private static readonly IReadOnlyList<IApiKeyProvider> _providers = ApiProviderRegistry.ScraperProviders;
        private static readonly Dictionary<string, Regex> _compiledRegexPatterns = new();
        private static readonly Dictionary<string, IApiKeyProvider> _patternToProviderMap = new();

        private static IServiceProvider? _serviceProvider;
        private static ILogger? _logger;
        private static IConfiguration? _configuration;
        private static CancellationTokenSource? _cancellationTokenSource;
        
        private const int MaxKeysPerRun = 1000;
        private const int DelayBetweenQueriesMs = 2000;

        static Scraper_Program()
        {
            // Build pattern to provider map and compile regex patterns
            foreach (var provider in _providers)
            {
                foreach (var pattern in provider.RegexPatterns)
                {
                    _patternToProviderMap[pattern] = provider;
                    try
                    {
                        var regexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;
                        _compiledRegexPatterns[pattern] = new Regex(pattern, regexOptions, TimeSpan.FromMilliseconds(5000));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: Failed to compile regex pattern '{pattern}': {ex.Message}");
                    }
                }
            }

            // Setup graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cancellationTokenSource?.Cancel();
                Console.WriteLine("\nGraceful shutdown initiated...");
            };
        }

        private static async Task Main(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            // Load configuration
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Check for export-only mode
            if (args != null && args.Contains("--export-only"))
            {
                Console.WriteLine("========================================");
                Console.WriteLine(" API Key Scraper - Export Mode          ");
                Console.WriteLine("========================================");
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
                    await ExportKeysToFilesAsync(dbContext);
                }
                Console.WriteLine("Export completed.");
                return;
            }

            var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
            _logger = loggerFactory?.CreateLogger("Scraper_Program");

            _logger?.LogInformation("========================================");
            _logger?.LogInformation(" API Key Scraper                        ");
            _logger?.LogInformation("========================================");
            Console.WriteLine("========================================");
            Console.WriteLine(" API Key Scraper                        ");
            Console.WriteLine("========================================");

            _logger?.LogInformation("Loaded {Count} scraper providers with {PatternCount} patterns",
                _providers.Count, _compiledRegexPatterns.Count);
            Console.WriteLine($"Loaded {_providers.Count} scraper providers with {_compiledRegexPatterns.Count} patterns");

            try
            {
                // Ensure DB is set up with initial data
                await DbSetup.EnsureDbSetupAsync(_serviceProvider);

                // Check for continuous mode setting
                bool continuousMode;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
                    continuousMode = bool.Parse((await dbContext.ApplicationSettings
                        .SingleOrDefaultAsync(x => x.Key == "ScraperContinuousMode", _cancellationTokenSource.Token))?.Value ?? "false");
                }

                if (continuousMode)
                {
                    _logger?.LogInformation("Running in CONTINUOUS MODE - will loop every 10 minutes");
                    Console.WriteLine("Running in CONTINUOUS MODE - will loop every 10 minutes");
                }
                else
                {
                    _logger?.LogInformation("Running in SINGLE RUN MODE - will exit after completion");
                    Console.WriteLine("Running in SINGLE RUN MODE - will exit after completion");
                }

                do
                {
                    try
                    {
                        await RunScrapingCycleAsync();

                        if (_cancellationTokenSource.Token.IsCancellationRequested)
                            break;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogInformation("Scraping cycle cancelled by user request");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error during scraping cycle. Will retry next cycle if in continuous mode.");
                        Console.WriteLine($"Error during scraping cycle: {ex.Message}");
                    }

                    if (continuousMode && !_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _logger?.LogInformation("Sleeping for 10 minutes before next scraping cycle...");
                        Console.WriteLine("Sleeping for 10 minutes before next scraping cycle...");

                        try
                        {
                            await Task.Delay(TimeSpan.FromMinutes(10), _cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                } while (continuousMode && !_cancellationTokenSource.Token.IsCancellationRequested);
            }
            finally
            {
                if (_serviceProvider is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (_serviceProvider is IDisposable disposable)
                    disposable.Dispose();
            }

            _logger?.LogInformation("Scraper shutdown completed");
            Console.WriteLine("Scraper shutdown completed");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Add database context
            // Add database context
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                ?? _configuration?.GetConnectionString("DefaultConnection")
                ?? "Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=devpassword;Port=5432";

            services.AddDbContext<DBContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsqlOptions.EnableRetryOnFailure(3);
                }));

            // Add HttpClient factory for GitLab and SourceGraph providers
            services.AddHttpClient("GitLabProviderClient", client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "UnsecuredAPIKeys-Scraper");
            });
            
            services.AddHttpClient("SourceGraphClient", client =>
            {
                client.BaseAddress = new Uri("https://sourcegraph.com/");
                client.DefaultRequestHeaders.Add("User-Agent", "UnsecuredAPIKeys-Scraper");
            });

            services.AddHttpClient();

            // Register search providers
            services.AddScoped<UnsecuredAPIKeys.Providers.Search_Providers.GitHubSearchProvider>();
            services.AddScoped<UnsecuredAPIKeys.Providers.Search_Providers.GitLabSearchProvider>();
            services.AddScoped<UnsecuredAPIKeys.Providers.Search_Providers.SourceGraphSearchProvider>();
        }

        private static async Task RunScrapingCycleAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            using var scope = _serviceProvider!.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

            // Check if scraper is enabled
            bool canRun = bool.Parse((await dbContext.ApplicationSettings
                .SingleOrDefaultAsync(x => x.Key == "AllowScraper", _cancellationTokenSource!.Token))?.Value ?? "false");

            if (!canRun)
            {
                _logger?.LogInformation("Scraper is disabled in application settings. Skipping cycle.");
                Console.WriteLine("Scraper is disabled in application settings. Skipping cycle.");
                return;
            }

            // Get all enabled search provider tokens
            var allTokens = await dbContext.SearchProviderTokens
                .Where(t => t.IsEnabled && !string.IsNullOrEmpty(t.Token))
                .ToListAsync(_cancellationTokenSource.Token);

            var githubTokens = allTokens.Where(t => t.SearchProvider == SearchProviderEnum.GitHub).ToList();

            // Fallback to environment variables if DB tokens are missing
            if (!githubTokens.Any())
            {
                // Single token
                var envToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                if (!string.IsNullOrEmpty(envToken))
                {
                    githubTokens.Add(new SearchProviderToken 
                    { 
                        SearchProvider = SearchProviderEnum.GitHub, 
                        Token = envToken, 
                        IsEnabled = true 
                    });
                }

                // Multiple tokens (Comma separated)
                var multiTokens = Environment.GetEnvironmentVariable("GITHUB_TOKENS");
                if (!string.IsNullOrEmpty(multiTokens))
                {
                    var tokens = multiTokens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var tokenStr in tokens)
                    {
                        // Avoid duplicates
                        if (!githubTokens.Any(t => t.Token == tokenStr))
                        {
                            githubTokens.Add(new SearchProviderToken 
                            { 
                                SearchProvider = SearchProviderEnum.GitHub, 
                                Token = tokenStr, 
                                IsEnabled = true 
                            });
                        }
                    }
                }
            }

            if (!githubTokens.Any())
            {
                _logger?.LogWarning("No GitHub search provider tokens available.");
                Console.WriteLine("Warning: No GitHub search provider tokens available.");
                return;
            }

            _logger?.LogInformation("Available GitHub Tokens: {Count}", githubTokens.Count);
            Console.WriteLine($"✓ Loaded {githubTokens.Count} GitHub Tokens for parallel processing");

            // Get enabled search queries
            // Fetch more queries since we have more power
            var queries = await dbContext.SearchQueries
                .Where(q => q.IsEnabled)
                .OrderBy(q => q.LastSearchUTC)
                .Take(50) // Process up to 50 queries per cycle (increased from 20)
                .ToListAsync(_cancellationTokenSource.Token);

            if (!queries.Any())
            {
                _logger?.LogInformation("No enabled search queries found.");
                Console.WriteLine("No enabled search queries found.");
                return;
            }

            _logger?.LogInformation("Processing {Count} search queries", queries.Count);
            Console.WriteLine($"Processing {queries.Count} search queries (Concurrency: 5)");

            int totalKeysFound = 0;
            int newKeysAdded = 0;
            int duplicatesSkipped = 0;
            int tokenIndex = -1;

            // Parallel execution
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 5, // Run 5 concurrent searches
                CancellationToken = _cancellationTokenSource.Token
            };

            await Parallel.ForEachAsync(queries, parallelOptions, async (query, ct) =>
            {
                // Create a dedicated scope for this thread
                using var threadScope = _serviceProvider.CreateScope();
                var threadDbContext = threadScope.ServiceProvider.GetRequiredService<DBContext>();
                
                // Re-attach query to this context
                var q = await threadDbContext.SearchQueries.FirstAsync(x => x.Id == query.Id, ct);

                // Round-robin token selection
                var currentTokenIndex = Interlocked.Increment(ref tokenIndex);
                var token = githubTokens[currentTokenIndex % githubTokens.Count];

                // _logger?.LogInformation("Thread {Id} using Token {TokenIdx} for '{Query}'", Environment.CurrentManagedThreadId, currentTokenIndex % githubTokens.Count, q.Query);

                try
                {
                    // Search GitHub
                    var repoReferences = await SearchGitHubAsync(q, token, threadDbContext);

                    foreach (var repoRef in repoReferences)
                    {
                        if (ct.IsCancellationRequested) break;

                        // Process Repo (Extract keys)
                        var extractedKeys = await ProcessRepoReferenceAsync(repoRef, token, threadDbContext);
                        
                        foreach (var extractedKey in extractedKeys)
                        {
                            var keyValue = extractedKey.key;
                            var provider = extractedKey.provider;
                            
                            Interlocked.Increment(ref totalKeysFound);

                            var existingKey = await threadDbContext.APIKeys
                                .FirstOrDefaultAsync(k => k.ApiKey == keyValue, ct);

                            if (existingKey != null)
                            {
                                existingKey.LastFoundUTC = DateTime.UtcNow;
                                Interlocked.Increment(ref duplicatesSkipped);
                            }
                            else
                            {
                                var newKey = new APIKey
                                {
                                    ApiKey = keyValue,
                                    ApiType = provider.ApiType,
                                    Status = ApiStatusEnum.Unverified,
                                    SearchProvider = SearchProviderEnum.GitHub,
                                    FirstFoundUTC = DateTime.UtcNow,
                                    LastFoundUTC = DateTime.UtcNow
                                };

                                threadDbContext.APIKeys.Add(newKey);
                                await threadDbContext.SaveChangesAsync(ct);

                                repoRef.APIKeyId = newKey.Id;
                                threadDbContext.RepoReferences.Add(repoRef);

                                Interlocked.Increment(ref newKeysAdded);
                                Console.WriteLine($"  + New {provider.ProviderName} key found!");
                            }
                            await threadDbContext.SaveChangesAsync(ct);
                        }
                    }

                    // Update LastSearchUTC
                    q.LastSearchUTC = DateTime.UtcNow;
                    await threadDbContext.SaveChangesAsync(ct);
                }
                catch (RateLimitExceededException ex)
                {
                    _logger?.LogWarning("Rate Limit Hit on Token {TokenIdx}. Reset: {Reset}", currentTokenIndex % githubTokens.Count, ex.Reset);
                    // Just skip this query for now, other threads continue
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing query '{Query}'", q.Query);
                }
            });

            stopwatch.Stop();

            _logger?.LogInformation("Scraping cycle completed in {Elapsed:F2} minutes. Total found: {Total}, New: {New}, Duplicates: {Dup}",
                stopwatch.Elapsed.TotalMinutes, totalKeysFound, newKeysAdded, duplicatesSkipped);
            Console.WriteLine($"\n========================================");
            Console.WriteLine($"Scraping cycle completed in {stopwatch.Elapsed.TotalMinutes:F2} minutes");
            Console.WriteLine($"Total keys found: {totalKeysFound}");
            Console.WriteLine($"New keys added: {newKeysAdded}");
            Console.WriteLine($"Duplicates skipped: {duplicatesSkipped}");
            Console.WriteLine($"========================================");

            // Export keys (using original db context for main thread)
            await ExportKeysToFilesAsync(dbContext);
        }

        private static async Task ExportKeysToFilesAsync(DBContext dbContext)
        {
            try
            {
                // Get all keys with their references
                var keys = await dbContext.APIKeys
                    .Include(k => k.References)
                    .OrderByDescending(k => k.FirstFoundUTC)
                    .ToListAsync(_cancellationTokenSource!.Token);

                if (!keys.Any())
                {
                    Console.WriteLine("No keys to export.");
                    return;
                }

                var outputDir = Path.GetFullPath(".");
                // Export to CSV (single persistent file)
                var csvPath = Path.Combine(outputDir, "founded_api_keys.csv");
                var csvLines = new List<string>
                {
                    "Id,ApiKey,Provider,Status,FirstFoundUTC,LastFoundUTC,SearchProvider,RepoURL,RepoOwner,RepoName,FilePath"
                };

                foreach (var key in keys)
                {
                    var refs = key.References.ToList();
                    if (refs.Any())
                    {
                        foreach (var r in refs)
                        {
                            csvLines.Add($"{key.Id},\"{key.ApiKey}\",{key.ApiType},{key.Status},{key.FirstFoundUTC:o},{key.LastFoundUTC:o},{key.SearchProvider},\"{r.RepoURL}\",\"{r.RepoOwner}\",\"{r.RepoName}\",\"{r.FilePath}\"");
                        }
                    }
                    else
                    {
                        csvLines.Add($"{key.Id},\"{key.ApiKey}\",{key.ApiType},{key.Status},{key.FirstFoundUTC:o},{key.LastFoundUTC:o},{key.SearchProvider},,,");
                    }
                }

                await File.WriteAllLinesAsync(csvPath, csvLines, _cancellationTokenSource.Token);

                // Also update the main founded_api_keys.txt
                var txtPath = Path.Combine(outputDir, "founded_api_keys.txt");
                var txtLines = new List<string>
                {
                    $"# UnsecuredAPIKeys - Found Keys Export",
                    $"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    $"# Total Keys: {keys.Count}",
                    ""
                };

                // Group keys by Status for better readability
                // Priority: Valid > Unverified > Invalid > Others
                var statusGroups = keys
                    .GroupBy(k => k.Status)
                    .OrderBy(g => g.Key == UnsecuredAPIKeys.Data.Common.ApiStatusEnum.Valid ? 0 :
                                  g.Key == UnsecuredAPIKeys.Data.Common.ApiStatusEnum.Unverified ? 1 : 2);

                foreach (var group in statusGroups)
                {
                    txtLines.Add($"=== {group.Key.ToString().ToUpper()} KEYS ({group.Count()}) ===");
                    txtLines.Add("");

                    // Within status, sort by Provider then Date
                    foreach (var key in group.OrderBy(k => k.ApiType).ThenByDescending(k => k.FirstFoundUTC))
                    {
                        txtLines.Add($"[{key.ApiType}] {key.ApiKey}");
                        txtLines.Add($"  Status: {key.Status}");
                        txtLines.Add($"  Found: {key.FirstFoundUTC:yyyy-MM-dd HH:mm}");
                        if (key.LastCheckedUTC.HasValue)
                        {
                             txtLines.Add($"  Verified: {key.LastCheckedUTC:yyyy-MM-dd HH:mm}");
                        }

                        if (key.References.Any())
                        {
                            var firstRef = key.References.First();
                            if (!string.IsNullOrEmpty(firstRef.RepoURL))
                                txtLines.Add($"  Source: {firstRef.RepoURL}");
                            if (!string.IsNullOrEmpty(firstRef.FilePath))
                                txtLines.Add($"  File: {firstRef.FilePath}");
                        }
                        txtLines.Add("");
                    }
                    txtLines.Add("----------------------------------------");
                    txtLines.Add("");
                }

                await File.WriteAllLinesAsync(txtPath, txtLines, _cancellationTokenSource.Token);

                Console.WriteLine($"\n📁 Keys exported to:");
                Console.WriteLine($"   CSV:  {csvPath}");
                Console.WriteLine($"   TXT:  {txtPath}");
                
                _logger?.LogInformation("Exported {Count} keys to CSV and TXT files", keys.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error exporting keys to files");
                Console.WriteLine($"Error exporting keys: {ex.Message}");
            }
        }

        private static async Task<List<RepoReference>> SearchGitHubAsync(
            SearchQuery query,
            SearchProviderToken token,
            DBContext dbContext)
        {
            var client = new GitHubClient(new ProductHeaderValue("UnsecuredAPIKeys-Scraper"))
            {
                Credentials = new Credentials(token.Token)
            };

            var results = new List<RepoReference>();
            int page = 1;
            const int perPage = 30; // Conservative to stay within rate limits

            try
            {
                var request = new SearchCodeRequest(query.Query)
                {
                    Page = page,
                    PerPage = perPage
                };

                var searchResult = await client.Search.SearchCode(request);

                if (page == 1)
                {
                    query.SearchResultsCount = searchResult.TotalCount;
                    dbContext.SearchQueries.Update(query);
                    await dbContext.SaveChangesAsync(_cancellationTokenSource!.Token);
                    
                    _logger?.LogInformation("Found {Count} total results for '{Query}'",
                        searchResult.TotalCount, query.Query);
                    Console.WriteLine($"  Found {searchResult.TotalCount} total results");
                }

                foreach (var item in searchResult.Items.Take(20)) // Limit to first 20 per query
                {
                    results.Add(new RepoReference
                    {
                        SearchQueryId = query.Id,
                        Provider = "GitHub",
                        RepoOwner = item.Repository?.Owner?.Login,
                        RepoName = item.Repository?.Name,
                        FilePath = item.Path,
                        FileURL = item.HtmlUrl,
                        ApiContentUrl = item.Url,
                        Branch = item.Repository?.DefaultBranch,
                        FileSHA = item.Sha,
                        FoundUTC = DateTime.UtcNow,
                        RepoURL = item.Repository?.HtmlUrl,
                        RepoDescription = item.Repository?.Description,
                        FileName = item.Name
                    });
                }
            }
            catch (RateLimitExceededException)
            {
                throw; // Re-throw to be handled by caller
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching GitHub for query '{Query}'", query.Query);
            }

            return results;
        }

        private static async Task<List<(string key, IApiKeyProvider provider)>> ProcessRepoReferenceAsync(
            RepoReference repoRef,
            SearchProviderToken token,
            DBContext dbContext)
        {
            var foundKeys = new List<(string key, IApiKeyProvider provider)>();

            if (string.IsNullOrEmpty(repoRef.ApiContentUrl))
                return foundKeys;

            try
            {
                // Fetch file content from GitHub API
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"token {token.Token}");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "UnsecuredAPIKeys-Scraper");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3.raw");

                var content = await httpClient.GetStringAsync(repoRef.ApiContentUrl, _cancellationTokenSource!.Token);

                if (string.IsNullOrWhiteSpace(content))
                    return foundKeys;

                // Extract keys using all provider patterns
                foreach (var (pattern, regex) in _compiledRegexPatterns)
                {
                    if (!_patternToProviderMap.TryGetValue(pattern, out var provider))
                        continue;

                    try
                    {
                        var matches = regex.Matches(content);
                        foreach (Match match in matches)
                        {
                            var keyValue = match.Value.Trim();
                            
                            // Basic validation
                            if (!string.IsNullOrWhiteSpace(keyValue) && keyValue.Length >= 10)
                            {
                                // Check if we haven't already found this key in this file
                                if (!foundKeys.Any(k => k.key == keyValue))
                                {
                                    foundKeys.Add((keyValue, provider));
                                }
                            }
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        _logger?.LogWarning("Regex timeout for pattern matching");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogWarning("Failed to fetch file content: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing repo reference");
            }

            return foundKeys;
        }
    }
}
