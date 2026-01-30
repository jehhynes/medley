using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorPlanArticleFragmentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_fragments");

            migrationBuilder.CreateTable(
                name: "plan_knowledge_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    knowledge_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    similarity_score = table.Column<double>(type: "double precision", nullable: false),
                    include = table.Column<bool>(type: "boolean", nullable: false),
                    reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_knowledge_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_knowledge_units_knowledge_units_knowledge_unit_id",
                        column: x => x.knowledge_unit_id,
                        principalTable: "knowledge_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plan_knowledge_units_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plan_knowledge_units_knowledge_unit_id",
                table: "plan_knowledge_units",
                column: "knowledge_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_knowledge_units_plan_id_knowledge_unit_id",
                table: "plan_knowledge_units",
                columns: new[] { "plan_id", "knowledge_unit_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_knowledge_units");

            migrationBuilder.CreateTable(
                name: "plan_fragments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    include = table.Column<bool>(type: "boolean", nullable: false),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    similarity_score = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_fragments", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_fragments_fragments_fragment_id",
                        column: x => x.fragment_id,
                        principalTable: "fragments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plan_fragments_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plan_fragments_fragment_id",
                table: "plan_fragments",
                column: "fragment_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_fragments_plan_id_fragment_id",
                table: "plan_fragments",
                columns: new[] { "plan_id", "fragment_id" },
                unique: true);
        }
    }
}
