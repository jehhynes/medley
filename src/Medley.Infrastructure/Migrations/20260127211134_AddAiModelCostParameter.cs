using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModelCostParameter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_model_cost_parameters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    input_cost_per_million = table.Column<decimal>(type: "numeric", nullable: true),
                    output_cost_per_million = table.Column<decimal>(type: "numeric", nullable: true),
                    embedding_cost_per_million = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_model_cost_parameters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_model_cost_parameters_model_name",
                table: "ai_model_cost_parameters",
                column: "model_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_model_cost_parameters");
        }
    }
}
