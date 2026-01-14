using Microsoft.EntityFrameworkCore;
using UnsecuredAPIKeys.Data.Models;

namespace UnsecuredAPIKeys.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }
        public DBContext() { }

        public DbSet<ApplicationSetting> ApplicationSettings { get; set; }
        public DbSet<SearchProviderToken> SearchProviderTokens { get; set; }
        public DbSet<APIKey> APIKeys { get; set; }
        public DbSet<RepoReference> RepoReferences { get; set; }
        public DbSet<SearchQuery> SearchQueries { get; set; }
        public DbSet<RateLimitLog> RateLimitLogs { get; set; }
        public DbSet<IssueSubmissionTracking> IssueSubmissionTrackings { get; set; }
        public DbSet<IssueVerification> IssueVerifications { get; set; }
        public DbSet<SnitchLeaderboard> SnitchLeaderboards { get; set; }
        public DbSet<Proxy> Proxies { get; set; }

        public DbSet<VerificationBatchResult> VerificationBatchResults { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserBan> UserBans { get; set; }
        
        // New entities for improved tracking
        public DbSet<KeyInvalidation> KeyInvalidations { get; set; }
        public DbSet<PatternEffectiveness> PatternEffectiveness { get; set; }
        public DbSet<KeyRotation> KeyRotations { get; set; }
        public DbSet<DonationTracking> DonationTrackings { get; set; }
        public DbSet<DonationSupporter> DonationSupporters { get; set; }
        public DbSet<VerificationBatch> VerificationBatches { get; set; }
        
        // Model-specific tracking
        public DbSet<ProviderModel> ProviderModels { get; set; }
        public DbSet<ApiKeyModel> ApiKeyModels { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add indexes for performance
            modelBuilder.Entity<APIKey>()
                .HasIndex(k => k.ApiKey)
                .HasDatabaseName("IX_APIKeys_ApiKey");

            modelBuilder.Entity<APIKey>()
                .HasIndex(k => new { k.Status, k.ApiType })
                .HasDatabaseName("IX_APIKeys_Status_ApiType");

            modelBuilder.Entity<APIKey>()
                .HasIndex(k => k.LastCheckedUTC)
                .HasDatabaseName("IX_APIKeys_LastCheckedUTC");

            modelBuilder.Entity<RepoReference>()
                .HasIndex(r => r.APIKeyId)
                .HasDatabaseName("IX_RepoReferences_ApiKeyId");

            // Note: IX_SearchQueries_IsEnabled_LastSearchUTC and IX_IssueSubmissionTrackings_ApiKeyId 
            // are already created in the AddPerformanceIndexes migration

            modelBuilder.Entity<KeyInvalidation>()
                .HasIndex(k => k.ApiKeyId)
                .HasDatabaseName("IX_KeyInvalidations_ApiKeyId");

            modelBuilder.Entity<KeyInvalidation>()
                .HasIndex(k => k.InvalidatedAt)
                .HasDatabaseName("IX_KeyInvalidations_InvalidatedAt");

            modelBuilder.Entity<PatternEffectiveness>()
                .HasIndex(p => p.ProviderName)
                .HasDatabaseName("IX_PatternEffectiveness_Provider");

            // Donation tracking indexes
            modelBuilder.Entity<DonationTracking>()
                .HasIndex(d => d.ClickedAt)
                .HasDatabaseName("IX_DonationTracking_ClickedAt");

            modelBuilder.Entity<DonationTracking>()
                .HasIndex(d => new { d.UserIP, d.ClickedAt })
                .HasDatabaseName("IX_DonationTracking_UserIP_ClickedAt");

            modelBuilder.Entity<DonationTracking>()
                .HasIndex(d => d.ConfirmedDonation)
                .HasDatabaseName("IX_DonationTracking_ConfirmedDonation");

            // Verification batch indexes
            modelBuilder.Entity<VerificationBatch>()
                .HasIndex(v => new { v.Status, v.LockExpiresAtUTC })
                .HasDatabaseName("IX_VerificationBatch_Status_LockExpires");

            modelBuilder.Entity<VerificationBatch>()
                .HasIndex(v => v.InstanceId)
                .HasDatabaseName("IX_VerificationBatch_InstanceId");

            modelBuilder.Entity<VerificationBatch>()
                .HasIndex(v => new { v.StartKeyId, v.EndKeyId })
                .HasDatabaseName("IX_VerificationBatch_KeyRange");

            // Configure relationships
            modelBuilder.Entity<KeyInvalidation>()
                .HasOne(k => k.ApiKey)
                .WithMany()
                .HasForeignKey(k => k.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<KeyRotation>()
                .HasOne(k => k.OldKey)
                .WithMany()
                .HasForeignKey(k => k.OldKeyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<KeyRotation>()
                .HasOne(k => k.NewKey)
                .WithMany()
                .HasForeignKey(k => k.NewKeyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Provider model indexes
            modelBuilder.Entity<ProviderModel>()
                .HasIndex(p => new { p.ApiType, p.ModelId })
                .IsUnique()
                .HasDatabaseName("IX_ProviderModels_ApiType_ModelId");

            modelBuilder.Entity<ProviderModel>()
                .HasIndex(p => p.ApiType)
                .HasDatabaseName("IX_ProviderModels_ApiType");

            modelBuilder.Entity<ProviderModel>()
                .HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_ProviderModels_IsActive");

            // ApiKeyModel relationships and indexes
            modelBuilder.Entity<ApiKeyModel>()
                .HasOne(akm => akm.ApiKey)
                .WithMany(ak => ak.ApiKeyModels)
                .HasForeignKey(akm => akm.ApiKeyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApiKeyModel>()
                .HasOne(akm => akm.ProviderModel)
                .WithMany(pm => pm.ApiKeyModels)
                .HasForeignKey(akm => akm.ProviderModelId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ApiKeyModel>()
                .HasIndex(akm => new { akm.ApiKeyId, akm.ProviderModelId })
                .IsUnique()
                .HasDatabaseName("IX_ApiKeyModels_ApiKeyId_ProviderModelId");

            modelBuilder.Entity<ApiKeyModel>()
                .HasIndex(akm => akm.HasAccess)
                .HasDatabaseName("IX_ApiKeyModels_HasAccess");
        }
    }
}
