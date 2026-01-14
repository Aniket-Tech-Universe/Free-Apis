# Import API Keys to Render PostgreSQL
# Run this script locally to seed your Render database with existing keys

param(
    [string]$ConnectionString = "Host=dpg-d5jkfg7gi27c73dsoheg-a.singapore-postgres.render.com;Database=unsecured_api;Username=unsecured_api_user;Password=xIfapmQeK0EZwosAY4AwQmVEFbMG1LSG;Port=5432;SSL Mode=Require;Trust Server Certificate=true"
)

# Install Npgsql if needed
if (-not (Get-Module -ListAvailable -Name Npgsql)) {
    Write-Host "Installing Npgsql module..." -ForegroundColor Yellow
    Install-Module -Name Npgsql -Scope CurrentUser -Force
}

Import-Module Npgsql

$csvPath = Join-Path $PSScriptRoot "founded_api_keys.csv"
if (-not (Test-Path $csvPath)) {
    Write-Error "CSV file not found: $csvPath"
    exit 1
}

Write-Host "Reading API keys from CSV..." -ForegroundColor Cyan
$keys = Import-Csv $csvPath

Write-Host "Connecting to Render PostgreSQL..." -ForegroundColor Cyan

try {
    $connection = New-Object Npgsql.NpgsqlConnection($ConnectionString)
    $connection.Open()
    Write-Host "Connected successfully!" -ForegroundColor Green

    # Check if table exists, create if not (EF migrations should handle this, but just in case)
    $checkTableCmd = $connection.CreateCommand()
    $checkTableCmd.CommandText = @"
    SELECT EXISTS (
        SELECT FROM information_schema.tables 
        WHERE table_name = 'APIKeys'
    );
"@
    $tableExists = $checkTableCmd.ExecuteScalar()
    
    if (-not $tableExists) {
        Write-Host "Creating APIKeys table..." -ForegroundColor Yellow
        $createTableCmd = $connection.CreateCommand()
        $createTableCmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS "APIKeys" (
            "Id" BIGSERIAL PRIMARY KEY,
            "ApiKey" TEXT NOT NULL,
            "Status" INTEGER NOT NULL DEFAULT 0,
            "ApiType" INTEGER NOT NULL DEFAULT 0,
            "SearchProvider" INTEGER NOT NULL DEFAULT 0,
            "LastCheckedUTC" TIMESTAMP,
            "FirstFoundUTC" TIMESTAMP NOT NULL,
            "LastFoundUTC" TIMESTAMP NOT NULL,
            "TimesDisplayed" INTEGER NOT NULL DEFAULT 0,
            "ErrorCount" INTEGER NOT NULL DEFAULT 0
        );
"@
        $createTableCmd.ExecuteNonQuery() | Out-Null
        Write-Host "Table created!" -ForegroundColor Green
    }

    # Map provider names to ApiTypeEnum values
    $apiTypeMap = @{
        "OpenAI"      = 0
        "GoogleAI"    = 1
        "DeepSeek"    = 2
        "ElevenLabs"  = 3
        "HuggingFace" = 4
        "Unknown"     = 99
    }
    
    # Map status names to ApiStatusEnum values
    $statusMap = @{
        "Valid"      = 1
        "Invalid"    = 2
        "Unverified" = 0
    }

    $inserted = 0
    $skipped = 0

    foreach ($key in $keys) {
        # Check if key already exists
        $checkCmd = $connection.CreateCommand()
        $checkCmd.CommandText = "SELECT COUNT(*) FROM ""APIKeys"" WHERE ""ApiKey"" = @apiKey"
        $checkCmd.Parameters.AddWithValue("apiKey", $key.ApiKey) | Out-Null
        $exists = [int]$checkCmd.ExecuteScalar()
        
        if ($exists -gt 0) {
            $skipped++
            continue
        }

        $apiType = if ($apiTypeMap.ContainsKey($key.Provider)) { $apiTypeMap[$key.Provider] } else { 99 }
        $status = if ($statusMap.ContainsKey($key.Status)) { $statusMap[$key.Status] } else { 0 }
        
        $insertCmd = $connection.CreateCommand()
        $insertCmd.CommandText = @"
        INSERT INTO "APIKeys" ("ApiKey", "Status", "ApiType", "SearchProvider", "FirstFoundUTC", "LastFoundUTC", "TimesDisplayed", "ErrorCount")
        VALUES (@apiKey, @status, @apiType, 0, @firstFound, @lastFound, 0, 0)
"@
        $insertCmd.Parameters.AddWithValue("apiKey", $key.ApiKey) | Out-Null
        $insertCmd.Parameters.AddWithValue("status", $status) | Out-Null
        $insertCmd.Parameters.AddWithValue("apiType", $apiType) | Out-Null
        $insertCmd.Parameters.AddWithValue("firstFound", [DateTime]::Parse($key.FirstFoundUTC)) | Out-Null
        $insertCmd.Parameters.AddWithValue("lastFound", [DateTime]::Parse($key.LastFoundUTC)) | Out-Null
        
        $insertCmd.ExecuteNonQuery() | Out-Null
        $inserted++
        Write-Host "Inserted: [$($key.Provider)] $($key.ApiKey.Substring(0, [Math]::Min(20, $key.ApiKey.Length)))..." -ForegroundColor Gray
    }

    Write-Host "`n✅ Import complete!" -ForegroundColor Green
    Write-Host "   Inserted: $inserted keys" -ForegroundColor Cyan
    Write-Host "   Skipped (duplicates): $skipped keys" -ForegroundColor Yellow

    $connection.Close()
}
catch {
    Write-Error "Error: $_"
    if ($connection) { $connection.Close() }
    exit 1
}
