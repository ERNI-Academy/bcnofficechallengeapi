using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bcnofficechallengeapi.Migrations
{
    public partial class ChangeIdsToGuid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS [ranking]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [user_sponsor_scans]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [users]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [sponsors]");

            migrationBuilder.Sql(@"CREATE TABLE [sponsors] ([id] uniqueidentifier NOT NULL DEFAULT NEWID(), [name] nvarchar(max) NOT NULL, [description] nvarchar(max) NULL, [url] nvarchar(max) NULL, [image] nvarchar(max) NULL, [points_value] int NOT NULL DEFAULT 0, CONSTRAINT [PK_sponsors] PRIMARY KEY ([id]))");
            migrationBuilder.Sql(@"CREATE TABLE [users] ([id] uniqueidentifier NOT NULL DEFAULT NEWID(), [name] nvarchar(max) NOT NULL, [email] nvarchar(max) NOT NULL, [company_name] nvarchar(max) NULL, [job_title] nvarchar(max) NULL, [points] int NOT NULL DEFAULT 0, [points_timestamp] datetime2 NULL, [linkedin] nvarchar(max) NULL, [password] nvarchar(max) NOT NULL, CONSTRAINT [PK_users] PRIMARY KEY ([id]))");
            migrationBuilder.Sql(@"CREATE TABLE [user_sponsor_scans] ([id] uniqueidentifier NOT NULL DEFAULT NEWID(), [user_id] uniqueidentifier NOT NULL, [sponsor_id] uniqueidentifier NOT NULL, [scanned_at] datetime2 NOT NULL, CONSTRAINT [PK_user_sponsor_scans] PRIMARY KEY ([id]), CONSTRAINT [UQ_user_sponsor] UNIQUE ([user_id], [sponsor_id]))");
            migrationBuilder.Sql("CREATE VIEW [ranking] AS SELECT [id], [name], [points], [email], [points_timestamp] FROM [users]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS [ranking]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [user_sponsor_scans]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [users]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [sponsors]");
        }
    }
}