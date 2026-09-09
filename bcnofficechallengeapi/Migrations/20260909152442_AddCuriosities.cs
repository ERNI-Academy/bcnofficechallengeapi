using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class AddCuriosities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "curiosities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sponsor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_curiosities", x => x.id);
                    table.ForeignKey(
                        name: "FK_curiosities_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalTable: "sponsors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_curiosity_views",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sponsor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    viewed_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_curiosity_views", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_curiosity_views_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalTable: "sponsors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_curiosity_views_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_curiosities_sponsor_id",
                table: "curiosities",
                column: "sponsor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_curiosity_views_sponsor_id",
                table: "user_curiosity_views",
                column: "sponsor_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_curiosity_views_user_id_sponsor_id",
                table: "user_curiosity_views",
                columns: new[] { "user_id", "sponsor_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "curiosities");

            migrationBuilder.DropTable(
                name: "user_curiosity_views");
        }
    }
}
