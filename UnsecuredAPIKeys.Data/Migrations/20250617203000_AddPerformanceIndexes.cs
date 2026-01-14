using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index on APIKeys.Status for filtering valid keys
            migrationBuilder.CreateIndex(
                name: "IX_APIKeys_Status",
                table: "APIKeys",
                column: "Status");

            // Index on APIKeys.ApiType for grouping and filtering
            migrationBuilder.CreateIndex(
                name: "IX_APIKeys_ApiType",
                table: "APIKeys",
                column: "ApiType");

            // Composite index for common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_APIKeys_Status_ApiType_LastCheckedUTC",
                table: "APIKeys",
                columns: new[] { "Status", "ApiType", "LastCheckedUTC" });

            // Index on SearchQueries for enabled queries ordered by last search
            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_IsEnabled_LastSearchUTC",
                table: "SearchQueries",
                columns: new[] { "IsEnabled", "LastSearchUTC" });

            // Index on IssueSubmissionTrackings.ApiKeyId for foreign key lookups
            migrationBuilder.CreateIndex(
                name: "IX_IssueSubmissionTrackings_ApiKeyId",
                table: "IssueSubmissionTrackings",
                column: "ApiKeyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_APIKeys_Status",
                table: "APIKeys");

            migrationBuilder.DropIndex(
                name: "IX_APIKeys_ApiType",
                table: "APIKeys");

            migrationBuilder.DropIndex(
                name: "IX_APIKeys_Status_ApiType_LastCheckedUTC",
                table: "APIKeys");

            migrationBuilder.DropIndex(
                name: "IX_SearchQueries_IsEnabled_LastSearchUTC",
                table: "SearchQueries");

            migrationBuilder.DropIndex(
                name: "IX_IssueSubmissionTrackings_ApiKeyId",
                table: "IssueSubmissionTrackings");
        }
    }
}