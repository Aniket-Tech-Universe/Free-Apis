using Microsoft.Extensions.Logging;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Providers._Base;
using UnsecuredAPIKeys.Providers._Interfaces;
using UnsecuredAPIKeys.Providers.Common;

namespace UnsecuredAPIKeys.Providers.DevTool_Providers
{
    [ApiProvider]
    public class GitLabProvider : BaseApiKeyProvider
    {
        public override string ProviderName => "GitLab";
        public override ApiTypeEnum ApiType => ApiTypeEnum.GitLab;

        public override IEnumerable<string> RegexPatterns =>
        [
            @"glpat-[0-9a-zA-Z_\-]{20,}", // Standard GitLab PAT
            @"glptt-[0-9a-zA-Z_\-]{40,}"  // GitLab Trigger Token
        ];

        public GitLabProvider(ILogger<GitLabProvider> logger) : base(logger)
        {
        }

        protected override async Task<ValidationResult> ValidateKeyWithHttpClientAsync(string apiKey, HttpClient client)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://gitlab.com/api/v4/user");
                request.Headers.Add("PRIVATE-TOKEN", apiKey);
                request.Headers.Add("User-Agent", "UnsecuredAPIKeys-Verifier");

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Success means valid token
                     var models = new List<ModelInfo> { new ModelInfo { ModelId = "gitlab_user", Description = "GitLab User" } };
                    return ValidationResult.Success(response.StatusCode, models);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return ValidationResult.IsUnauthorized(response.StatusCode);
                }

                return ValidationResult.HasProviderSpecificError($"HTTP {response.StatusCode}");

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating GitLab key");
                return ValidationResult.HasNetworkError(ex.Message);
            }
        }
    }
}
