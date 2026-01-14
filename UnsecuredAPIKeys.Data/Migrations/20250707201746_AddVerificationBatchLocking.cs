using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationBatchLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstanceId = table.Column<string>(type: "text", nullable: false),
                    LockedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockExpiresAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartKeyId = table.Column<long>(type: "bigint", nullable: false),
                    EndKeyId = table.Column<long>(type: "bigint", nullable: false),
                    KeyCount = table.Column<int>(type: "integer", nullable: false),
                    ProcessingStartedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingCompletedAtUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidKeys = table.Column<int>(type: "integer", nullable: true),
                    InvalidKeys = table.Column<int>(type: "integer", nullable: true),
                    SkippedKeys = table.Column<int>(type: "integer", nullable: true),
                    ErrorKeys = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationBatches", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationBatch_InstanceId",
                table: "VerificationBatches",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationBatch_KeyRange",
                table: "VerificationBatches",
                columns: new[] { "StartKeyId", "EndKeyId" });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationBatch_Status_LockExpires",
                table: "VerificationBatches",
                columns: new[] { "Status", "LockExpiresAtUTC" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationBatches");
        }
    }
}
