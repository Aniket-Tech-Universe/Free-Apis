# Automation Pipeline for API Key Scraper & Verifier

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   API Key Scraper & Verifier Pipeline" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 1. Check Prerequisites
if (-not $env:GITHUB_TOKEN) {
    Write-Host "Error: GITHUB_TOKEN environment variable is not set." -ForegroundColor Red
    Write-Host "Please set it using: `$env:GITHUB_TOKEN = 'your_token'" -ForegroundColor Yellow
    exit 1
}

# 2. Build Scraper & Verifier
Write-Host "`n[1/3] Building Projects..." -ForegroundColor Green
dotnet build UnsecuredAPIKeys.Bots.Scraper\UnsecuredAPIKeys.Bots.Scraper.csproj --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Scraper build failed. Exiting." -ForegroundColor Red
    exit 1
}
dotnet build UnsecuredAPIKeys.Bots.Verifier\UnsecuredAPIKeys.Bots.Verifier.csproj --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Verifier build failed. Exiting." -ForegroundColor Red
    exit 1
}

# 3. Run Scraper
Write-Host "`n[2/3] Running Scraper Bot..." -ForegroundColor Green
dotnet run --project UnsecuredAPIKeys.Bots.Scraper\UnsecuredAPIKeys.Bots.Scraper.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Scraper encountered an error. Continuing to verifier..." -ForegroundColor Yellow
}

# 4. Run Verifier
Write-Host "`n[3/4] Running Verifier Bot..." -ForegroundColor Cyan
dotnet run --project UnsecuredAPIKeys.Bots.Verifier\UnsecuredAPIKeys.Bots.Verifier.csproj
if ($LASTEXITCODE -ne 0) { Write-Host "Verifier run failed." -ForegroundColor Red; exit 1 }

# Refresh the export file with verification results
Write-Host "`n[4/4] Refreshing Export File..." -ForegroundColor Cyan
dotnet run --project UnsecuredAPIKeys.Bots.Scraper\UnsecuredAPIKeys.Bots.Scraper.csproj -- --export-only
if ($LASTEXITCODE -ne 0) { Write-Host "Export refresh failed." -ForegroundColor Red; exit 1 }

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "   Pipeline Completed Successfully" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Show export locations
$outputDir = Get-Location
Write-Host "Txt Export: $outputDir\founded_api_keys.txt" -ForegroundColor Yellow
Write-Host "Csv Export: $outputDir\founded_api_keys.csv" -ForegroundColor Yellow
