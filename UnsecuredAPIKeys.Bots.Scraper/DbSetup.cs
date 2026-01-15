using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.Bots.Scraper
{
    public class DbSetup
    {
        public static async Task EnsureDbSetupAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();

            // Enable scraper
            var allowScraper = await dbContext.ApplicationSettings.SingleOrDefaultAsync(x => x.Key == "AllowScraper");
            if (allowScraper == null)
            {
                dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = "AllowScraper", Value = "true" });
            }
            else
            {
                dbContext.ApplicationSettings.Remove(allowScraper);
                dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = "AllowScraper", Value = "true" });
            }
            await dbContext.SaveChangesAsync();

            // Enable verifier
            var allowVerifier = await dbContext.ApplicationSettings.SingleOrDefaultAsync(x => x.Key == "AllowVerifier");
            if (allowVerifier == null)
            {
                dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = "AllowVerifier", Value = "true" });
            }
            else
            {
                if (allowVerifier.Value != "true")
                {
                    dbContext.ApplicationSettings.Remove(allowVerifier);
                    dbContext.ApplicationSettings.Add(new ApplicationSetting { Key = "AllowVerifier", Value = "true" });
                }
            }
            await dbContext.SaveChangesAsync();

            // Add a test SearchProviderToken (GitHub example)
            // Read token from environment (GITHUB_TOKEN). Avoid hardcoding secrets in source.
            var githubTokenValue = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            var githubToken = await dbContext.SearchProviderTokens.SingleOrDefaultAsync(t => t.SearchProvider == UnsecuredAPIKeys.Data.Common.SearchProviderEnum.GitHub);
            if (!string.IsNullOrWhiteSpace(githubTokenValue))
            {
                if (githubToken == null)
                {
                    dbContext.SearchProviderTokens.Add(new SearchProviderToken {
                        Token = githubTokenValue,
                        SearchProvider = UnsecuredAPIKeys.Data.Common.SearchProviderEnum.GitHub,
                        IsEnabled = true
                    });
                    Console.WriteLine("✓ GitHub token configured");
                }
                else
                {
                    githubToken.Token = githubTokenValue;
                    githubToken.IsEnabled = true;
                    Console.WriteLine("✓ GitHub token updated");
                }
            }
            else
            {
                Console.WriteLine("  GITHUB_TOKEN not set (optional)");
            }

            // GitLab token
            var gitlabTokenValue = Environment.GetEnvironmentVariable("GITLAB_TOKEN");
            var gitlabToken = await dbContext.SearchProviderTokens.SingleOrDefaultAsync(t => t.SearchProvider == UnsecuredAPIKeys.Data.Common.SearchProviderEnum.GitLab);
            if (!string.IsNullOrWhiteSpace(gitlabTokenValue))
            {
                if (gitlabToken == null)
                {
                    dbContext.SearchProviderTokens.Add(new SearchProviderToken {
                        Token = gitlabTokenValue,
                        SearchProvider = UnsecuredAPIKeys.Data.Common.SearchProviderEnum.GitLab,
                        IsEnabled = true
                    });
                    Console.WriteLine("✓ GitLab token configured");
                }
                else
                {
                    gitlabToken.Token = gitlabTokenValue;
                    gitlabToken.IsEnabled = true;
                    Console.WriteLine("✓ GitLab token updated");
                }
            }
            else
            {
                Console.WriteLine("  GITLAB_TOKEN not set (optional)");
            }

            // SourceGraph token
            var sourcegraphTokenValue = Environment.GetEnvironmentVariable("SOURCEGRAPH_TOKEN");
            var sourcegraphToken = await dbContext.SearchProviderTokens.SingleOrDefaultAsync(t => t.SearchProvider == UnsecuredAPIKeys.Data.Common.SearchProviderEnum.SourceGraph);
            if (!string.IsNullOrWhiteSpace(sourcegraphTokenValue))
            {
                if (sourcegraphToken == null)
                {
                    dbContext.SearchProviderTokens.Add(new SearchProviderToken {
                        Token = sourcegraphTokenValue,
                        SearchProvider = UnsecuredAPIKeys.Data.Common.SearchProviderEnum.SourceGraph,
                        IsEnabled = true
                    });
                    Console.WriteLine("✓ SourceGraph token configured");
                }
                else
                {
                    sourcegraphToken.Token = sourcegraphTokenValue;
                    sourcegraphToken.IsEnabled = true;
                    Console.WriteLine("✓ SourceGraph token updated");
                }
            }
            else
            {
                Console.WriteLine("  SOURCEGRAPH_TOKEN not set (optional)");
            }
            await dbContext.SaveChangesAsync();

            // Add priority-ordered search queries
            var priorityQueries = new[]
            {
                // Priority 1: Google AI (search first - oldest dates)
                ("AIzaSy", DateTime.UtcNow.AddYears(-5)),
                ("GOOGLE_API_KEY", DateTime.UtcNow.AddYears(-5).AddDays(1)),
                ("gemini api key", DateTime.UtcNow.AddYears(-5).AddDays(2)),
                // Targeting config files (High Yield)
                ("filename:.env AIzaSy", DateTime.UtcNow.AddYears(-5).AddDays(3)),
                ("filename:config.js AIzaSy", DateTime.UtcNow.AddYears(-5).AddDays(4)),
                ("filename:secrets.yaml AIzaSy", DateTime.UtcNow.AddYears(-5).AddDays(5)),
                ("path:.github/workflows AIzaSy", DateTime.UtcNow.AddYears(-5).AddDays(6)),

                // Priority 2: OpenAI
                ("sk-proj-", DateTime.UtcNow.AddYears(-4)),
                ("OPENAI_API_KEY", DateTime.UtcNow.AddYears(-4).AddDays(1)),
                ("openai_key", DateTime.UtcNow.AddYears(-4).AddDays(2)),
                ("filename:.env sk-proj-", DateTime.UtcNow.AddYears(-4).AddDays(3)),

                // Priority 3: Anthropic
                ("sk-ant-api", DateTime.UtcNow.AddYears(-3)),
                ("ANTHROPIC_API_KEY", DateTime.UtcNow.AddYears(-3).AddDays(1)),
                ("filename:.env sk-ant-api", DateTime.UtcNow.AddYears(-3).AddDays(2)),

                // Priority 4: Other AI providers
                ("MISTRAL_API_KEY", DateTime.UtcNow.AddYears(-2)),
                ("GROQ_API_KEY", DateTime.UtcNow.AddYears(-2).AddDays(1)),
                ("COHERE_API_KEY", DateTime.UtcNow.AddYears(-2).AddDays(2)),
                ("HUGGINGFACE_TOKEN", DateTime.UtcNow.AddYears(-2).AddDays(3)),
                ("ELEVEN_LABS_API", DateTime.UtcNow.AddYears(-2).AddDays(4)),
                ("DEEPSEEK_API_KEY", DateTime.UtcNow.AddYears(-2).AddDays(5)),
                ("PERPLEXITY_API_KEY", DateTime.UtcNow.AddYears(-2).AddDays(6)),
                ("REPLICATE_API_TOKEN", DateTime.UtcNow.AddYears(-2).AddDays(7)),
                ("STABILITY_API_KEY", DateTime.UtcNow.AddYears(-2).AddDays(8)),

                // Priority 5: General fallback
                ("api_key", DateTime.UtcNow.AddYears(-1))
            };

            foreach (var (queryText, lastSearchUtc) in priorityQueries)
            {
                var existingQuery = await dbContext.SearchQueries.SingleOrDefaultAsync(q => q.Query == queryText);
                if (existingQuery == null)
                {
                    dbContext.SearchQueries.Add(new SearchQuery {
                        Query = queryText,
                        IsEnabled = true,
                        LastSearchUTC = lastSearchUtc
                    });
                    Console.WriteLine($"  + Added query: {queryText}");
                }
            }
            await dbContext.SaveChangesAsync();
            
            var totalQueries = await dbContext.SearchQueries.CountAsync();
            Console.WriteLine($"✓ Total search queries: {totalQueries}");
        }
    }
}
