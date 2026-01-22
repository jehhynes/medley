using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeakerExtraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "speakers_extracted",
                table: "sources",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "enable_speaker_extraction",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "speakers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_internal = table.Column<bool>(type: "boolean", nullable: true),
                    trust_level = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_speakers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "source_speakers",
                columns: table => new
                {
                    sources_id = table.Column<Guid>(type: "uuid", nullable: false),
                    speakers_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_speakers", x => new { x.sources_id, x.speakers_id });
                    table.ForeignKey(
                        name: "fk_source_speakers_sources_sources_id",
                        column: x => x.sources_id,
                        principalTable: "sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_source_speakers_speakers_speakers_id",
                        column: x => x.speakers_id,
                        principalTable: "speakers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_source_speakers_speakers_id",
                table: "source_speakers",
                column: "speakers_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "source_speakers");

            migrationBuilder.DropTable(
                name: "speakers");

            migrationBuilder.DropColumn(
                name: "speakers_extracted",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "enable_speaker_extraction",
                table: "organizations");
        }
    }
}
