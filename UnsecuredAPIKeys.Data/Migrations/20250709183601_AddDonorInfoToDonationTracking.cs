using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonorInfoToDonationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DonorEmail",
                table: "DonationTrackings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonorFirstName",
                table: "DonationTrackings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonorLastName",
                table: "DonationTrackings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IPNRawMessage",
                table: "DonationTrackings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IPNVerified",
                table: "DonationTrackings",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayPalPayerId",
                table: "DonationTrackings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "DonationTrackings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DonorEmail",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "DonorFirstName",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "DonorLastName",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "IPNRawMessage",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "IPNVerified",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "PayPalPayerId",
                table: "DonationTrackings");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "DonationTrackings");
        }
    }
}
