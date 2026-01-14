# Analyze unmatched API keys in the verification bot

# First, let's see what providers we have
Write-Host "=== ANALYZING UNMATCHED KEYS ===" -ForegroundColor Cyan
Write-Host ""

# List all AI providers
Write-Host "Current AI Providers registered:" -ForegroundColor Yellow
$providers = @(
    "AnthropicProvider",
    "CohereProvider", 
    "DeepSeekProvider",
    "ElevenLabsProvider",
    "GoogleProvider",
    "GroqProvider",
    "HuggingFaceProvider",
    "MistralAIProvider",
    "OpenAIProvider",
    "OpenRouterProvider",
    "PerplexityAIProvider",
    "ReplicateProvider",
    "StabilityAIProvider",
    "TogetherAIProvider"
)

$providers | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
Write-Host "Common API key patterns that might be missing:" -ForegroundColor Yellow
Write-Host "  - Gemini API keys"
Write-Host "  - Claude API keys (different from Anthropic format)"
Write-Host "  - Azure OpenAI keys"
Write-Host "  - AWS Bedrock keys"
Write-Host "  - Vertex AI keys"
Write-Host "  - Local LLM server keys"
Write-Host "  - Custom/Enterprise API keys"
Write-Host ""

Write-Host "The message 'Processing 1000 keys with unknown pattern' means:" -ForegroundColor Green
Write-Host "  1. The bot found 1000 keys that don't match ANY provider's regex patterns"
Write-Host "  2. These keys will be tested against ALL providers (slow!)"
Write-Host "  3. This significantly impacts performance"
Write-Host ""

Write-Host "To fix this issue:" -ForegroundColor Magenta
Write-Host "  1. Query the database to see what these unmatched keys look like"
Write-Host "  2. Identify which services they belong to"
Write-Host "  3. Either:"
Write-Host "     a) Add new providers for missing services"
Write-Host "     b) Update existing provider regex patterns"
Write-Host "     c) Mark false-positive keys as invalid"
Write-Host ""

Write-Host "SQL query to find unmatched key patterns:" -ForegroundColor Cyan
Write-Host @"
-- This query would help identify unmatched keys
SELECT 
    SUBSTRING(ApiKey, 1, 10) as key_prefix,
    COUNT(*) as count,
    STRING_AGG(DISTINCT SearchProvider::text, ', ') as search_providers
FROM APIKeys
WHERE Status NOT IN ('Invalid', 'Removed', 'FlaggedForRemoval')
GROUP BY SUBSTRING(ApiKey, 1, 10)
ORDER BY count DESC
LIMIT 50;
"@
