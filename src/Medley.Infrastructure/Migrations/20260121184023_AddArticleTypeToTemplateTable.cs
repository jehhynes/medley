using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleTypeToTemplateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "article_type_id",
                table: "templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_templates_article_type_id",
                table: "templates",
                column: "article_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_templates_article_types_article_type_id",
                table: "templates",
                column: "article_type_id",
                principalTable: "article_types",
                principalColumn: "id");


            // Add unique constraint on (type, article_type_id)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_templates_type_article_type_id_unique 
                ON templates (type, article_type_id) 
                WHERE article_type_id IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_templates_type_unique 
                ON templates (type) 
                WHERE article_type_id IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_templates_article_types_article_type_id",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "ix_templates_article_type_id",
                table: "templates");

            migrationBuilder.DropColumn(
                name: "article_type_id",
                table: "templates");
        }
    }
}
