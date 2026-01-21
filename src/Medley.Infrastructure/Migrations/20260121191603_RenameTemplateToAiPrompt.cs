using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTemplateToAiPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    article_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_prompts", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_prompts_article_types_article_type_id",
                        column: x => x.article_type_id,
                        principalTable: "article_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_prompts_article_type_id",
                table: "ai_prompts",
                column: "article_type_id");

            // Migrate data from templates to ai_prompts
            migrationBuilder.Sql(@"
                INSERT INTO ai_prompts (id, type, content, article_type_id, last_modified_at, created_at)
                SELECT id, type, content, article_type_id, last_modified_at, created_at
                FROM templates;
            ");

            migrationBuilder.DropTable(
                name: "templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_prompts");

            migrationBuilder.CreateTable(
                name: "templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_type_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_templates_article_types_article_type_id",
                        column: x => x.article_type_id,
                        principalTable: "article_types",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_templates_article_type_id",
                table: "templates",
                column: "article_type_id");
        }
    }
}
