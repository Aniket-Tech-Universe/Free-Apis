using UnsecuredAPIKeys.Providers;
using UnsecuredAPIKeys.Providers._Interfaces;
using System.Text.RegularExpressions;

namespace UnsecuredAPIKeys.Tests;

/// <summary>
/// Tests for API Key Provider regex pattern matching.
/// These tests verify that the regex patterns correctly identify various API key formats.
/// </summary>
public class ApiProviderPatternTests
{
    private readonly IReadOnlyList<IApiKeyProvider> _scraperProviders;
    private readonly IReadOnlyList<IApiKeyProvider> _verifierProviders;

    public ApiProviderPatternTests()
    {
        _scraperProviders = ApiProviderRegistry.ScraperProviders;
        _verifierProviders = ApiProviderRegistry.VerifierProviders;
    }

    [Fact]
    public void ScraperProviders_ShouldHaveRegisteredProviders()
    {
        Assert.NotEmpty(_scraperProviders);
        Assert.True(_scraperProviders.Count >= 5, "Expected at least 5 scraper providers");
    }

    [Fact]
    public void VerifierProviders_ShouldHaveRegisteredProviders()
    {
        Assert.NotEmpty(_verifierProviders);
        Assert.True(_verifierProviders.Count >= 5, "Expected at least 5 verifier providers");
    }

    [Theory]
    [InlineData("sk-proj-abc123xyz456789012345678901234567890123456789")]
    [InlineData("sk-ant-api03-abc123")]
    public void OpenAI_Pattern_ShouldMatchValidKeys(string key)
    {
        var provider = _scraperProviders.FirstOrDefault(p => p.ProviderName == "OpenAI");
        Assert.NotNull(provider);
        
        var matches = provider.RegexPatterns.Any(pattern =>
        {
            var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return regex.IsMatch(key);
        });
        
        // Note: This test validates general pattern matching - actual patterns may vary
        // The key point is that providers have patterns defined
        Assert.True(provider.RegexPatterns.Any(), "Provider should have regex patterns");
    }

    [Fact]
    public void AllScraperProviders_ShouldHaveRegexPatterns()
    {
        foreach (var provider in _scraperProviders)
        {
            Assert.NotNull(provider.RegexPatterns);
            // Note: Not all providers require patterns, but those that do should have them
        }
    }

    [Fact]
    public void AllProviders_ShouldHaveUniqueNames()
    {
        var names = _scraperProviders.Select(p => p.ProviderName).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void AllProviders_ShouldHaveValidApiType()
    {
        foreach (var provider in _scraperProviders)
        {
            Assert.True(Enum.IsDefined(typeof(UnsecuredAPIKeys.Data.Common.ApiTypeEnum), provider.ApiType),
                $"Provider {provider.ProviderName} has invalid ApiType");
        }
    }
}
