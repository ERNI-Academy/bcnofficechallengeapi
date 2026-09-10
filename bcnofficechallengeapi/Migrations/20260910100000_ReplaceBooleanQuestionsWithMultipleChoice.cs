using System;
using bcnofficechallengeapi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260910100000_ReplaceBooleanQuestionsWithMultipleChoice")]
    public partial class ReplaceBooleanQuestionsWithMultipleChoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE users
                SET points = 0,
                    points_timestamp = NULL;

                DELETE FROM user_sponsor_scans;
                """);

            migrationBuilder.DropTable(
                name: "user_sponsor_scan_answers");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sponsor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    points = table.Column<int>(type: "int", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    is_correct = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.id);
                    table.ForeignKey(
                        name: "FK_question_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sponsor_scan_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_sponsor_scan_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    question_text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    selected_option_ids = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    correct_option_ids = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                name: "IX_questions_sponsor_id",
                table: "questions",
                column: "sponsor_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_options_question_id_sort_order",
                table: "question_options",
                columns: new[] { "question_id", "sort_order" });

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
                name: "question_options");

            migrationBuilder.DropTable(
                name: "user_sponsor_scan_answers");

            migrationBuilder.DropTable(
                name: "questions");

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
                name: "IX_questions_sponsor_id_sort_order",
                table: "questions",
                columns: new[] { "sponsor_id", "sort_order" });

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
    }
}
