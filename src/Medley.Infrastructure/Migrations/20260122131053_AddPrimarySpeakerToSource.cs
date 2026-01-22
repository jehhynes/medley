using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimarySpeakerToSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "primary_speaker_id",
                table: "sources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sources_primary_speaker_id",
                table: "sources",
                column: "primary_speaker_id");

            migrationBuilder.AddForeignKey(
                name: "fk_sources_speakers_primary_speaker_id",
                table: "sources",
                column: "primary_speaker_id",
                principalTable: "speakers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sources_speakers_primary_speaker_id",
                table: "sources");

            migrationBuilder.DropIndex(
                name: "ix_sources_primary_speaker_id",
                table: "sources");

            migrationBuilder.DropColumn(
                name: "primary_speaker_id",
                table: "sources");
        }
    }
}
