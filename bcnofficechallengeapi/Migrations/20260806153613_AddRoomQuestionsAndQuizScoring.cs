using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomQuestionsAndQuizScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "maximum_points",
                table: "user_sponsor_scans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "points_awarded",
                table: "user_sponsor_scans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE scans
                SET points_awarded = sponsors.points_value,
                    maximum_points = sponsors.points_value
                FROM user_sponsor_scans AS scans
                INNER JOIN sponsors ON sponsors.id = scans.sponsor_id;

                IF NOT EXISTS (SELECT 1 FROM sponsors WHERE name = 'Kitchen')
                    INSERT INTO sponsors (id, qr_id, name, description, url, image, points_value)
                    VALUES (NEWID(), NEWID(), 'Kitchen', '', NULL, '', 0);

                IF NOT EXISTS (SELECT 1 FROM sponsors WHERE name = 'Toilets')
                    INSERT INTO sponsors (id, qr_id, name, description, url, image, points_value)
                    VALUES (NEWID(), NEWID(), 'Toilets', '', NULL, '', 0);
                """);

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sponsor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    correct_answer = table.Column<bool>(type: "bit", nullable: false),
                    points = table.Column<int>(type: "int", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.id);
                    table.ForeignKey(
                        name: "FK_questions_sponsors_sponsor_id",
                        column: x => x.sponsor_id,
                        principalTable: "sponsors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sponsor_scans_sponsor_id",
                table: "user_sponsor_scans",
                column: "sponsor_id");

            migrationBuilder.CreateIndex(
                name: "IX_questions_sponsor_id_sort_order",
                table: "questions",
                columns: new[] { "sponsor_id", "sort_order" });

            migrationBuilder.AddForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans",
                column: "sponsor_id",
                principalTable: "sponsors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_sponsor_scans_users_user_id",
                table: "user_sponsor_scans",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_sponsor_scans_sponsors_sponsor_id",
                table: "user_sponsor_scans");

            migrationBuilder.DropForeignKey(
                name: "FK_user_sponsor_scans_users_user_id",
                table: "user_sponsor_scans");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_sponsor_scans_sponsor_id",
                table: "user_sponsor_scans");

            migrationBuilder.DropColumn(
                name: "maximum_points",
                table: "user_sponsor_scans");

            migrationBuilder.DropColumn(
                name: "points_awarded",
                table: "user_sponsor_scans");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
