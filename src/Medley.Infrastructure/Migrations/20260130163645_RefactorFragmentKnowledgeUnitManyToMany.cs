using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFragmentKnowledgeUnitManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fragments_knowledge_units_knowledge_unit_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_fragments_knowledge_unit_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "knowledge_unit_id",
                table: "fragments");

            migrationBuilder.CreateTable(
                name: "fragment_knowledge_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    knowledge_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fragment_knowledge_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_fragment_knowledge_units_fragments_fragment_id",
                        column: x => x.fragment_id,
                        principalTable: "fragments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fragment_knowledge_units_knowledge_units_knowledge_unit_id",
                        column: x => x.knowledge_unit_id,
                        principalTable: "knowledge_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_fragment_knowledge_units_fragment_id_knowledge_unit_id",
                table: "fragment_knowledge_units",
                columns: new[] { "fragment_id", "knowledge_unit_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fragment_knowledge_units_knowledge_unit_id",
                table: "fragment_knowledge_units",
                column: "knowledge_unit_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fragment_knowledge_units");

            migrationBuilder.AddColumn<Guid>(
                name: "knowledge_unit_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fragments_knowledge_unit_id",
                table: "fragments",
                column: "knowledge_unit_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_knowledge_units_knowledge_unit_id",
                table: "fragments",
                column: "knowledge_unit_id",
                principalTable: "knowledge_units",
                principalColumn: "id");
        }
    }
}
