using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchProviderToAPIKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchProvider",
                table: "APIKeys",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SearchProvider",
                table: "APIKeys");
        }
    }
}
