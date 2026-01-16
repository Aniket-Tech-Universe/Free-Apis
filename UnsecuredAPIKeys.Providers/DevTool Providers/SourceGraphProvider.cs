using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Providers._Base;
using UnsecuredAPIKeys.Providers._Interfaces;
using UnsecuredAPIKeys.Providers.Common;

namespace UnsecuredAPIKeys.Providers.DevTool_Providers
{
    [ApiProvider]
    public class SourceGraphProvider : BaseApiKeyProvider
    {
        public override string ProviderName => "SourceGraph";
        public override ApiTypeEnum ApiType => ApiTypeEnum.SourceGraph;

        public override IEnumerable<string> RegexPatterns =>
        [
            @"sgp_[a-fA-F0-9]{40,}" // SourceGraph Access Token
        ];

        public SourceGraphProvider(ILogger<SourceGraphProvider> logger) : base(logger)
        {
        }

        protected override async Task<ValidationResult> ValidateKeyWithHttpClientAsync(string apiKey, HttpClient client)
        {
            try
            {
                var query = new { query = "query { currentUser { username } }" };
                var json = JsonSerializer.Serialize(query);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, "https://sourcegraph.com/.api/graphql");
                request.Headers.Add("Authorization", $"token {apiKey}");
                request.Content = content;

                using var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (responseBody.Contains("currentUser") && responseBody.Contains("username"))
                    {
                         var models = new List<ModelInfo> { new ModelInfo { ModelId = "sourcegraph_user", Description = "SourceGraph User" } };
                        return ValidationResult.Success(response.StatusCode, models);
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || 
                    response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return ValidationResult.IsUnauthorized(response.StatusCode);
                }

                return ValidationResult.HasProviderSpecificError($"HTTP {response.StatusCode}");

            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating SourceGraph key");
                return ValidationResult.HasNetworkError(ex.Message);
            }
        }
    }
}
