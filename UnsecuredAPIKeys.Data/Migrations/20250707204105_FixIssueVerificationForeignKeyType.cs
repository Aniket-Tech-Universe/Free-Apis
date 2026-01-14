using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixIssueVerificationForeignKeyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
                name: "IssueSubmissionTrackingId",
                table: "IssueVerifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IssueVerifications_IssueSubmissionTrackings_IssueSubmission~",
                table: "IssueVerifications");

            migrationBuilder.DropIndex(
                name: "IX_IssueVerifications_IssueSubmissionTrackingId",
                table: "IssueVerifications");

            migrationBuilder.AlterColumn<long>(
                name: "IssueSubmissionTrackingId",
                table: "IssueVerifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

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
    }
}
