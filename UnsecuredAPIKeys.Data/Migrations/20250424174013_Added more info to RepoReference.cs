using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedmoreinfotoRepoReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimesDisplayed",
                table: "RepoReferences",
                newName: "LineNumber");

            migrationBuilder.AddColumn<string>(
                name: "CodeContext",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileSHA",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileURL",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepoDescription",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "RepoId",
                table: "RepoReferences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RepoName",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RepoOwner",
                table: "RepoReferences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SearchQueryId",
                table: "RepoReferences",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodeContext",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "FileSHA",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "FileURL",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "RepoDescription",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "RepoId",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "RepoName",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "RepoOwner",
                table: "RepoReferences");

            migrationBuilder.DropColumn(
                name: "SearchQueryId",
                table: "RepoReferences");

            migrationBuilder.RenameColumn(
                name: "LineNumber",
                table: "RepoReferences",
                newName: "TimesDisplayed");
        }
    }
}
