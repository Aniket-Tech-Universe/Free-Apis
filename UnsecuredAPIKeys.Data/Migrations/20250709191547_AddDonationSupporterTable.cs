using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationSupporterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonationSupporters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DonationTrackingId = table.Column<long>(type: "bigint", nullable: false),
                    PayPalTransactionId = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DiscordUserId = table.Column<long>(type: "bigint", nullable: true),
                    DiscordUserId1 = table.Column<int>(type: "integer", nullable: true),
                    DiscordUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShowOnSupportersPage = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserIP = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationSupporters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationSupporters_DiscordUsers_DiscordUserId1",
                        column: x => x.DiscordUserId1,
                        principalTable: "DiscordUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DonationSupporters_DonationTrackings_DonationTrackingId",
                        column: x => x.DonationTrackingId,
                        principalTable: "DonationTrackings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationSupporters_DiscordUserId1",
                table: "DonationSupporters",
                column: "DiscordUserId1");

            migrationBuilder.CreateIndex(
                name: "IX_DonationSupporters_DonationTrackingId",
                table: "DonationSupporters",
                column: "DonationTrackingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationSupporters");
        }
    }
}
