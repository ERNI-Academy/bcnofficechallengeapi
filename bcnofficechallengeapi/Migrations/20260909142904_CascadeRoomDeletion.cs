using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class CascadeRoomDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans");

            migrationBuilder.AddForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans",
                column: "sponsor_id",
                principalTable: "sponsors",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans");

            migrationBuilder.AddForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans",
                column: "sponsor_id",
                principalTable: "sponsors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
