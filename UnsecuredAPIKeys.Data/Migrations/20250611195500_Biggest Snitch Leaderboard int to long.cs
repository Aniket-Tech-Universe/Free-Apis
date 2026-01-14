using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class BiggestSnitchLeaderboardinttolong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                table: "IssueVerifications");

            migrationBuilder.DropIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId",
                table: "IssueVerifications");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "SnitchLeaderboards",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<long>(
                name: "IssueSubmissionTrackingId",
                table: "IssueVerifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "GitHubIssueNumber",
                table: "IssueVerifications",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "IssueVerifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "IssueSubmissionTrackingId1",
                table: "IssueVerifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId1",
                table: "IssueVerifications",
                column: "IssueSubmissionTrackingId1");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                table: "IssueVerifications",
                column: "IssueSubmissionTrackingId1",
                principalTable: "IssueSubmissionTrackings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                table: "IssueVerifications");

            migrationBuilder.DropIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId1",
                table: "IssueVerifications");

            migrationBuilder.DropColumn(
                name: "IssueSubmissionTrackingId1",
                table: "IssueVerifications");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SnitchLeaderboards",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<int>(
                name: "IssueSubmissionTrackingId",
                table: "IssueVerifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "GitHubIssueNumber",
                table: "IssueVerifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "IssueVerifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId",
                table: "IssueVerifications",
                column: "IssueSubmissionTrackingId");

            migrationBuilder.AddForeignKey(
                name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                table: "IssueVerifications",
                column: "IssueSubmissionTrackingId",
                principalTable: "IssueSubmissionTrackings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
