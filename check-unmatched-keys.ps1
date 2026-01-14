# Script to analyze unmatched API keys in the database

$connectionString = "Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=your_passwordca32"

# Load the providers assembly to get regex patterns
Add-Type -Path "UnsecuredAPIKeys.Providers/bin/Debug/net9.0/UnsecuredAPIKeys.Providers.dll"
Add-Type -Path "UnsecuredAPIKeys.Data/bin/Debug/net9.0/UnsecuredAPIKeys.Data.dll"

# Get all verifier providers and their patterns
$providers = [UnsecuredAPIKeys.Providers.ApiProviderRegistry]::VerifierProviders
$patterns = @{}

foreach ($provider in $providers) {
    foreach ($pattern in $provider.RegexPatterns) {
        $patterns[$pattern] = $provider.ProviderName
    }
}

Write-Host "Found $($patterns.Count) regex patterns from $($providers.Count) providers" -ForegroundColor Green
Write-Host ""

# Connect to database and analyze keys
try {
    # This is a simplified check - in production you'd want to query the database
    Write-Host "Sample regex patterns currently registered:" -ForegroundColor Yellow
    $patterns.Keys | Select-Object -First 10 | ForEach-Object {
        Write-Host "  Provider: $($patterns[$_])"
        Write-Host "  Pattern: $_"
        Write-Host ""
    }
    
    Write-Host "To find unmatched keys, you would need to:" -ForegroundColor Cyan
    Write-Host "1. Query all API keys from the database"
    Write-Host "2. Test each key against all patterns"
    Write-Host "3. Identify keys that don't match any pattern"
    Write-Host ""
    Write-Host "Common reasons for unmatched keys:" -ForegroundColor Yellow
    Write-Host "- New API services without providers"
    Write-Host "- Changed API key formats"
    Write-Host "- False positives from the scraper"
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
}
