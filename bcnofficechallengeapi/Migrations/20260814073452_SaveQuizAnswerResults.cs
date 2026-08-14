using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class SaveQuizAnswerResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_sponsor_scan_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_sponsor_scan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    selected_answer = table.Column<bool>(type: "bit", nullable: false),
                    correct_answer = table.Column<bool>(type: "bit", nullable: false),
                    is_correct = table.Column<bool>(type: "bit", nullable: false),
                    points_awarded = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sponsor_scan_answers", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sponsor_scan_answers_user_sponsor_scans_user_sponsor_scan_id",
                        column: x => x.user_sponsor_scan_id,
                        principalTable: "user_sponsor_scans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_sponsor_scan_answers_user_sponsor_scan_id_question_id",
                table: "user_sponsor_scan_answers",
                columns: new[] { "user_sponsor_scan_id", "question_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_sponsor_scan_answers");
        }
    }
}
