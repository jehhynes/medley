using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepresentativeFragmentToFragment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "representative_fragment_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fragments_representative_fragment_id",
                table: "fragments",
                column: "representative_fragment_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragments_representative_fragment_id",
                table: "fragments",
                column: "representative_fragment_id",
                principalTable: "fragments",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragments_representative_fragment_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_fragments_representative_fragment_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "representative_fragment_id",
                table: "fragments");
        }
    }
}
