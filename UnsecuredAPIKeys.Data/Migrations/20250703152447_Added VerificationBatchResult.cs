using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UnsecuredAPIKeys.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedVerificationBatchResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationBatchResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VerificationDateUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    ValidKeys = table.Column<int>(type: "integer", nullable: false),
                    InvalidKeys = table.Column<int>(type: "integer", nullable: false),
                    SkippedKeys = table.Column<int>(type: "integer", nullable: false),
                    TimeTakenInMinutes = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationBatchResults", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationBatchResults");
        }
    }
}
