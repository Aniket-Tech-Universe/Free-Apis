// Quick console app to import CSV to database
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;

var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
    "Host=dpg-d5jkfg7gi27c73dsoheg-a.singapore-postgres.render.com;Database=unsecured_api;Username=unsecured_api_user;Password=xIfapmQeK0EZwosAY4AwQmVEFbMG1LSG;Port=5432;SSL Mode=Require;Trust Server Certificate=true";

var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
optionsBuilder.UseNpgsql(connectionString);

using var context = new DBContext(optionsBuilder.Options);

// Ensure database is created
await context.Database.EnsureCreatedAsync();

var csvPath = Path.Combine(AppContext.BaseDirectory, "../../../../founded_api_keys.csv");
if (!File.Exists(csvPath))
{
    csvPath = "founded_api_keys.csv";
}

Console.WriteLine($"Reading from: {csvPath}");
var lines = File.ReadAllLines(csvPath).Skip(1); // Skip header

var providerMap = new Dictionary<string, ApiTypeEnum>(StringComparer.OrdinalIgnoreCase)
{
    ["OpenAI"] = ApiTypeEnum.OpenAI,
    ["GoogleAI"] = ApiTypeEnum.GoogleAI,
    ["DeepSeek"] = ApiTypeEnum.DeepSeek,
    ["ElevenLabs"] = ApiTypeEnum.ElevenLabs,
    ["HuggingFace"] = ApiTypeEnum.HuggingFace,
};

var statusMap = new Dictionary<string, ApiStatusEnum>(StringComparer.OrdinalIgnoreCase)
{
    ["Valid"] = ApiStatusEnum.Valid,
    ["Invalid"] = ApiStatusEnum.Invalid,
    ["Unverified"] = ApiStatusEnum.Unverified,
};

int inserted = 0, skipped = 0;

foreach (var line in lines)
{
    if (string.IsNullOrWhiteSpace(line)) continue;
    
    var parts = line.Split(',');
    if (parts.Length < 5) continue;
    
    var apiKey = parts[1].Trim('"');
    var provider = parts[2].Trim('"');
    var status = parts[3].Trim('"');
    var firstFound = DateTime.Parse(parts[4].Trim('"'), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    var lastFound = DateTime.Parse(parts[5].Trim('"'), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    
    // Check if already exists
    if (await context.APIKeys.AnyAsync(k => k.ApiKey == apiKey))
    {
        skipped++;
        continue;
    }
    
    var key = new APIKey
    {
        ApiKey = apiKey,
        ApiType = providerMap.GetValueOrDefault(provider, ApiTypeEnum.Unknown),
        Status = statusMap.GetValueOrDefault(status, ApiStatusEnum.Unverified),
        FirstFoundUTC = firstFound,
        LastFoundUTC = lastFound,
        SearchProvider = SearchProviderEnum.GitHub,
        TimesDisplayed = 0,
        ErrorCount = 0
    };
    
    context.APIKeys.Add(key);
    inserted++;
    Console.WriteLine($"Added: [{provider}] {apiKey[..Math.Min(20, apiKey.Length)]}...");
}

await context.SaveChangesAsync();

Console.WriteLine($"\n✅ Import complete!");
Console.WriteLine($"   Inserted: {inserted} keys");
Console.WriteLine($"   Skipped: {skipped} keys");
