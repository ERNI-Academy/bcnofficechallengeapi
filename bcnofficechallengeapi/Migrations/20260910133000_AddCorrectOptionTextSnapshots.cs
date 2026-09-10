using bcnofficechallengeapi.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260910133000_AddCorrectOptionTextSnapshots")]
    public partial class AddCorrectOptionTextSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "correct_option_texts",
                table: "user_sponsor_scan_answers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE [scan_answer]
                SET [correct_option_texts] = COALESCE(
                    (
                        SELECT N'[' + STRING_AGG(
                            N'"' + STRING_ESCAPE([option].[text], 'json') + N'"',
                            N','
                        ) WITHIN GROUP (ORDER BY [option].[sort_order], [option].[id]) + N']'
                        FROM OPENJSON([scan_answer].[correct_option_ids]) AS [stored]
                        INNER JOIN [question_options] AS [option]
                            ON [option].[id] = TRY_CONVERT(uniqueidentifier, [stored].[value])
                    ),
                    N'[]'
                )
                FROM [user_sponsor_scan_answers] AS [scan_answer];
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "correct_option_texts",
                table: "user_sponsor_scan_answers");
        }
    }
}
