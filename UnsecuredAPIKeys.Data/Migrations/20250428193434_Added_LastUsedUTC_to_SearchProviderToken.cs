using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class Added_LastUsedUTC_to_SearchProviderToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedUTC",
                table: "SearchProviderTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ApiContentUrl",
                table: "RepoReferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "RepoReferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "RepoReferences",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUsedUTC",
                table: "SearchProviderTokens");

            migrationBuilder.DropColumn(
                name: "ApiContentUrl",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "Branch",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "RepoReferences");
        }
    }
}
