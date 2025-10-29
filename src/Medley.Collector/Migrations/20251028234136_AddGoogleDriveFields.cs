using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleDriveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MeetingTranscripts");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MeetingTranscripts",
                newName: "DownloadedAt");

            migrationBuilder.RenameColumn(
                name: "MeetingId",
                table: "MeetingTranscripts",
                newName: "ExternalId");

            migrationBuilder.RenameColumn(
                name: "FullJson",
                table: "MeetingTranscripts",
                newName: "Content");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingTranscripts_MeetingId",
                table: "MeetingTranscripts",
                newName: "IX_MeetingTranscripts_ExternalId");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "MeetingTranscripts",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "Fellow.ai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Source",
                table: "MeetingTranscripts");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "MeetingTranscripts",
                newName: "MeetingId");

            migrationBuilder.RenameColumn(
                name: "DownloadedAt",
                table: "MeetingTranscripts",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "MeetingTranscripts",
                newName: "FullJson");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingTranscripts_ExternalId",
                table: "MeetingTranscripts",
                newName: "IX_MeetingTranscripts_MeetingId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "MeetingTranscripts",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
