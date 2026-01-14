using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class Addedresultscounttosearchquery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchResultsCount",
                table: "SearchQueries",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchResultsCount",
                table: "SearchQueries");
        }
    }
}
