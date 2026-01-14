using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubUserInfoToIssueVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GitHubAvatarUrl",
                table: "IssueVerifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubDisplayName",
                table: "IssueVerifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GitHubUserId",
                table: "IssueVerifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubUsername",
                table: "IssueVerifications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubAvatarUrl",
                table: "IssueVerifications");

            migrationBuilder.DropColumn(
                name: "GitHubDisplayName",
                table: "IssueVerifications");

            migrationBuilder.DropColumn(
                name: "GitHubUserId",
                table: "IssueVerifications");

            migrationBuilder.DropColumn(
                name: "GitHubUsername",
                table: "IssueVerifications");
        }
    }
}
