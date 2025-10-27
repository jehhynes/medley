using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.CollectorUtil.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingTranscripts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MeetingTranscripts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MeetingId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Participants = table.Column<string>(type: "TEXT", nullable: true),
                    ApiKeyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FullJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTranscripts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_MeetingId_ApiKeyName",
                table: "MeetingTranscripts",
                columns: new[] { "MeetingId", "ApiKeyName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingTranscripts");
        }
    }
}
