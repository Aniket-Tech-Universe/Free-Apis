using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModelTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProviderModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApiType = table.Column<int>(type: "integer", nullable: false),
                    ModelId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InputTokenLimit = table.Column<long>(type: "bigint", nullable: true),
                    OutputTokenLimit = table.Column<long>(type: "bigint", nullable: true),
                    SupportedMethods = table.Column<string>(type: "text", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    TopP = table.Column<float>(type: "real", nullable: true),
                    TopK = table.Column<int>(type: "integer", nullable: true),
                    MaxTemperature = table.Column<float>(type: "real", nullable: true),
                    ModelGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "boolean", nullable: false),
                    DeprecatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstSeenUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeyModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApiKeyId = table.Column<long>(type: "bigint", nullable: false),
                    ProviderModelId = table.Column<long>(type: "bigint", nullable: false),
                    DiscoveredUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastVerifiedUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasAccess = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeyModels_APIKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "APIKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiKeyModels_ProviderModels_ProviderModelId",
                        column: x => x.ProviderModelId,
                        principalTable: "ProviderModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyModels_ApiKeyId_ProviderModelId",
                table: "ApiKeyModels",
                columns: new[] { "ApiKeyId", "ProviderModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyModels_HasAccess",
                table: "ApiKeyModels",
                column: "HasAccess");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyModels_ProviderModelId",
                table: "ApiKeyModels",
                column: "ProviderModelId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderModels_ApiType",
                table: "ProviderModels",
                column: "ApiType");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderModels_ApiType_ModelId",
                table: "ProviderModels",
                columns: new[] { "ApiType", "ModelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderModels_IsActive",
                table: "ProviderModels",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyModels");

            migrationBuilder.DropTable(
                name: "ProviderModels");
        }
    }
}
