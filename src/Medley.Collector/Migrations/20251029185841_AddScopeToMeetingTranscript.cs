using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeToMeetingTranscript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "MeetingTranscripts",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "MeetingTranscripts");
        }
    }
}
