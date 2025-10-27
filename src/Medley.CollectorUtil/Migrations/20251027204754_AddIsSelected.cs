using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.CollectorUtil.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSelected : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "MeetingTranscripts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "MeetingTranscripts");
        }
    }
}
