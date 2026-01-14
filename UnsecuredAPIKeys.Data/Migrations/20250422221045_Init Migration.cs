using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APIKeys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApiKey = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: -99),
                    ApiType = table.Column<int>(type: "integer", nullable: false, defaultValue: -99),
                    LastCheckedUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastValidUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimesDisplayed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APIKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepoReferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    APIKeyId = table.Column<long>(type: "bigint", nullable: false),
                    RepoURL = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FoundUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimesDisplayed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepoReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepoReferences_APIKeys_APIKeyId",
                        column: x => x.APIKeyId,
                        principalTable: "APIKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepoReferences_APIKeyId",
                table: "RepoReferences",
                column: "APIKeyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepoReferences");

            migrationBuilder.DropTable(
                name: "APIKeys");
        }
    }
}
