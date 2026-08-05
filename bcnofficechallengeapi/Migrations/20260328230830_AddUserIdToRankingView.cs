using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToRankingView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER VIEW [ranking] AS
                SELECT [id], [name], [points], [email], [points_timestamp]
                FROM [users]
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER VIEW [ranking] AS
                SELECT [name], [points], [email], [points_timestamp]
                FROM [users]
            ");
        }
    }
}
