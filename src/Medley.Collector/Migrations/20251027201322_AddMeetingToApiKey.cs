using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Collector.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingToApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MeetingTranscripts_MeetingId_ApiKeyName",
                table: "MeetingTranscripts");

            migrationBuilder.DropColumn(
                name: "ApiKeyName",
                table: "MeetingTranscripts");

            migrationBuilder.CreateTable(
                name: "MeetingTranscriptApiKeys",
                columns: table => new
                {
                    ApiKeysId = table.Column<int>(type: "INTEGER", nullable: false),
                    MeetingTranscriptsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingTranscriptApiKeys", x => new { x.ApiKeysId, x.MeetingTranscriptsId });
                    table.ForeignKey(
                        name: "FK_MeetingTranscriptApiKeys_ApiKeys_ApiKeysId",
                        column: x => x.ApiKeysId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingTranscriptApiKeys_MeetingTranscripts_MeetingTranscriptsId",
                        column: x => x.MeetingTranscriptsId,
                        principalTable: "MeetingTranscripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_MeetingId",
                table: "MeetingTranscripts",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscriptApiKeys_MeetingTranscriptsId",
                table: "MeetingTranscriptApiKeys",
                column: "MeetingTranscriptsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeetingTranscriptApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_MeetingTranscripts_MeetingId",
                table: "MeetingTranscripts");

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyName",
                table: "MeetingTranscripts",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingTranscripts_MeetingId_ApiKeyName",
                table: "MeetingTranscripts",
                columns: new[] { "MeetingId", "ApiKeyName" });
        }
    }
}
