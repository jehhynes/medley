using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceDetail",
                table: "MeetingTranscripts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceDetail",
                table: "MeetingTranscripts");
        }
    }
}
