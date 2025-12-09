using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertTemplateTypeToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add a temporary integer column
            migrationBuilder.AddColumn<int>(
                name: "TypeNew",
                table: "Templates",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Convert existing string values to enum integers
            migrationBuilder.Sql(@"
                UPDATE ""Templates"" 
                SET ""TypeNew"" = CASE 
                    WHEN ""Type"" = 'FragmentExtraction' THEN 1
                    WHEN ""Type"" = 'ArticleGeneration' THEN 2
                    WHEN ""Type"" = 'InsightGeneration' THEN 3
                    WHEN ""Type"" = 'SourceSummary' THEN 4
                    ELSE 1
                END;
            ");

            // Drop the old string column
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Templates");

            // Rename the new column to Type
            migrationBuilder.RenameColumn(
                name: "TypeNew",
                table: "Templates",
                newName: "Type");

            // Add Description column
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Templates",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Add LastModifiedAt column
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Templates",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Templates");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Templates");

            // Add back string column
            migrationBuilder.AddColumn<string>(
                name: "TypeOld",
                table: "Templates",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Convert enum integers back to string values
            migrationBuilder.Sql(@"
                UPDATE ""Templates"" 
                SET ""TypeOld"" = CASE 
                    WHEN ""Type"" = 1 THEN 'FragmentExtraction'
                    WHEN ""Type"" = 2 THEN 'ArticleGeneration'
                    WHEN ""Type"" = 3 THEN 'InsightGeneration'
                    WHEN ""Type"" = 4 THEN 'SourceSummary'
                    ELSE 'FragmentExtraction'
                END;
            ");

            // Drop the integer column
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Templates");

            // Rename back
            migrationBuilder.RenameColumn(
                name: "TypeOld",
                table: "Templates",
                newName: "Type");
        }
    }
}
