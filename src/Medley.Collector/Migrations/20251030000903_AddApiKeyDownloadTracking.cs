using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyDownloadTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DownloadedThroughDate",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsed",
                table: "ApiKeys",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadedThroughDate",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "LastUsed",
                table: "ApiKeys");
        }
    }
}
