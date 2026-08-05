using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSponsorScans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_sponsor_scans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    sponsor_id = table.Column<int>(type: "int", nullable: false),
                    scanned_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sponsor_scans", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_sponsor_scans_user_id_sponsor_id",
                table: "user_sponsor_scans",
                columns: new[] { "user_id", "sponsor_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_sponsor_scans");
        }
    }
}
