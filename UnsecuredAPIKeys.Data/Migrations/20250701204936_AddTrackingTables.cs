using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SearchQueries_IsEnabled_LastSearchUTC",
                table: "SearchQueries");

            migrationBuilder.DropIndex(
                name: "IX_IssueSubmissionTrackings_ApiKeyId",
                table: "IssueSubmissionTrackings");

            migrationBuilder.DropIndex(
                name: "IX_APIKeys_Status_ApiType_LastCheckedUTC",
                table: "APIKeys");

            migrationBuilder.CreateTable(
                name: "KeyInvalidations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApiKeyId = table.Column<long>(type: "bigint", nullable: false),
                    InvalidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvalidationReason = table.Column<string>(type: "text", nullable: true),
                    WasValid = table.Column<bool>(type: "boolean", nullable: false),
                    DaysActive = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedFixed = table.Column<bool>(type: "boolean", nullable: false),
                    FixedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousStatus = table.Column<string>(type: "text", nullable: true),
                    HttpStatusCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyInvalidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyInvalidations_APIKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "APIKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KeyRotations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OldKeyId = table.Column<long>(type: "bigint", nullable: false),
                    NewKeyId = table.Column<long>(type: "bigint", nullable: false),
                    RotatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RepoUrl = table.Column<string>(type: "text", nullable: false),
                    OldKeyDaysActive = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyRotations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeyRotations_APIKeys_NewKeyId",
                        column: x => x.NewKeyId,
                        principalTable: "APIKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeyRotations_APIKeys_OldKeyId",
                        column: x => x.OldKeyId,
                        principalTable: "APIKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatternEffectiveness",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Pattern = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: false),
                    TotalMatches = table.Column<int>(type: "integer", nullable: false),
                    ValidKeys = table.Column<int>(type: "integer", nullable: false),
                    InvalidKeys = table.Column<int>(type: "integer", nullable: false),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MostSuccessfulFileTypes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatternEffectiveness", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeyInvalidations_ApiKeyId",
                table: "KeyInvalidations",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyInvalidations_InvalidatedAt",
                table: "KeyInvalidations",
                column: "InvalidatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KeyRotations_NewKeyId",
                table: "KeyRotations",
                column: "NewKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_KeyRotations_OldKeyId",
                table: "KeyRotations",
                column: "OldKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_PatternEffectiveness_Provider",
                table: "PatternEffectiveness",
                column: "ProviderName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KeyInvalidations");

            migrationBuilder.DropTable(
                name: "KeyRotations");

            migrationBuilder.DropTable(
                name: "PatternEffectiveness");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_IsEnabled_LastSearchUTC",
                table: "SearchQueries",
                columns: new[] { "IsEnabled", "LastSearchUTC" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueSubmissionTrackings_ApiKeyId",
                table: "IssueSubmissionTrackings",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_APIKeys_Status_ApiType_LastCheckedUTC",
                table: "APIKeys",
                columns: new[] { "Status", "ApiType", "LastCheckedUTC" });
        }
    }
}
