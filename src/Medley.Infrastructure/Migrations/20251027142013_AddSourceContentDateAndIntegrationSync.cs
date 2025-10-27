using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceContentDateAndIntegrationSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Sources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Date",
                table: "Sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IntegrationId",
                table: "Sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InitialSyncCompleted",
                table: "Integrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "IntegrationId",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "InitialSyncCompleted",
                table: "Integrations");
        }
    }
}
