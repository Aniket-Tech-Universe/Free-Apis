using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.WebAPI.Hubs;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Get connection string - prioritize environment variable for production (Render)
            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                ?? builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Database=UnsecuredAPIKeys;Username=postgres;Password=your_password;Port=5432";

            builder.Services.AddDbContext<DBContext>(options =>
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    npgsqlOptions.EnableRetryOnFailure(3);
                });
            });
            builder.Services.AddScoped<ISystemdService, SystemdService>();
            builder.Services.AddMemoryCache();
            builder.Services.AddControllers();
            
            // Add OpenAPI/Swagger
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info = new()
                    {
                        Title = "UnsecuredAPIKeys API",
                        Version = "v1",
                        Description = "API for managing and retrieving exposed API keys"
                    };
                    return Task.CompletedTask;
                });
            });
            
            // Add SignalR with better configuration
            builder.Services.AddSignalR(options =>
            {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
            });
            
            // Add Display Count Service
            builder.Services.AddSingleton<IDisplayCountService, DisplayCountService>();
            
            // Add Active User Service
            builder.Services.AddSingleton<IActiveUserService, ActiveUserService>();
            
            // Add Rate Limit Cleanup Service
            builder.Services.AddScoped<IRateLimitCleanupService, RateLimitCleanupService>();
            builder.Services.AddHostedService<RateLimitCleanupHostedService>();
            
            // Add Discord Service
            builder.Services.AddHttpClient<IDiscordService, DiscordService>();
            builder.Services.AddScoped<IDiscordService, DiscordService>();
            
            // Add Discord Role Service
            builder.Services.AddHttpClient<IDiscordRoleService, DiscordRoleService>();
            builder.Services.AddScoped<IDiscordRoleService, DiscordRoleService>();
            
            // Add response compression
            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
            });
            
            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });
            
            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = System.IO.Compression.CompressionLevel.Optimal;
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", builder =>
                {
                    var allowedOrigins = new List<string>
                    {
                        "http://localhost:3000",
                        "http://localhost:3001",
                        "http://localhost:5173",
                        "https://free-unsecured-apis.vercel.app"  // User's Vercel deployment
                    };

                    // Add production domains from environment variables
                    var productionDomain = Environment.GetEnvironmentVariable("PRODUCTION_DOMAIN");
                    if (!string.IsNullOrEmpty(productionDomain))
                    {
                        allowedOrigins.Add($"https://{productionDomain}");
                        allowedOrigins.Add($"https://www.{productionDomain}");
                    }

                    builder
                        .WithOrigins(allowedOrigins.ToArray())
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            var app = builder.Build();

            // Apply any pending Migrations
            using (var scope = app.Services.CreateScope())
            {
                var servicesTmp = scope.ServiceProvider;
                try
                {
                    var context = servicesTmp.GetRequiredService<DBContext>();

                    if (!(await context.Database.GetPendingMigrationsAsync().ConfigureAwait(true)).Any())
                    {
                        Log.Information("No Pending migrations for the database. Continuing.");
                    }
                    else
                    {
                        Log.Information("Applying database migrations...");
                        await context.Database.MigrateAsync().ConfigureAwait(true);
                        Log.Information("Database migrations applied successfully");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while applying database migrations");
                    throw;
                }
            }

            app.UseCors("AllowedOrigins");
            
            // Enable response compression
            app.UseResponseCompression();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "UnsecuredAPIKeys API";
                    options.Theme = ScalarTheme.Purple;
                    options.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<StatsHub>("/hubs/stats");

            // Initialize display count service
            var displayCountService = app.Services.GetRequiredService<IDisplayCountService>();
            await displayCountService.InitializeAsync();

            await app.RunAsync();
        }
    }
}
