using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class BiggestSnitchLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IssueSubmissionTrackingId = table.Column<int>(type: "integer", nullable: false),
                    RepoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssueTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GitHubIssueNumber = table.Column<int>(type: "integer", nullable: true),
                    GitHubIssueUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssueClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastCheckedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmitterIP = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                        column: x => x.IssueSubmissionTrackingId,
                        principalTable: "IssueSubmissionTrackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SnitchLeaderboards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TotalIssuesSubmitted = table.Column<int>(type: "integer", nullable: false),
                    OpenIssuesSubmitted = table.Column<int>(type: "integer", nullable: false),
                    ClosedIssuesSubmitted = table.Column<int>(type: "integer", nullable: false),
                    FirstSubmissionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSubmissionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalRepositoriesAffected = table.Column<int>(type: "integer", nullable: false),
                    FavoriteApiType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ConsecutiveDaysActive = table.Column<int>(type: "integer", nullable: false),
                    SnitchScore = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnitchLeaderboards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId",
                table: "IssueVerifications",
                column: "IssueSubmissionTrackingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueVerifications");

            migrationBuilder.DropTable(
                name: "SnitchLeaderboards");
        }
    }
}
