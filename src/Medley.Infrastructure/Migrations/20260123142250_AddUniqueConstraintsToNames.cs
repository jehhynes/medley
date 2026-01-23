using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ai_prompts_fragment_category_fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragment_category_fragment_category_id",
                table: "fragments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fragment_category",
                table: "fragment_category");

            migrationBuilder.RenameTable(
                name: "fragment_category",
                newName: "fragment_categories");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fragment_categories",
                table: "fragment_categories",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_article_types_name",
                table: "article_types",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fragment_categories_name",
                table: "fragment_categories",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_ai_prompts_fragment_categories_fragment_category_id",
                table: "ai_prompts",
                column: "fragment_category_id",
                principalTable: "fragment_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragment_categories_fragment_category_id",
                table: "fragments",
                column: "fragment_category_id",
                principalTable: "fragment_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_ai_prompts_fragment_categories_fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragment_categories_fragment_category_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_article_types_name",
                table: "article_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fragment_categories",
                table: "fragment_categories");

            migrationBuilder.DropIndex(
                name: "ix_fragment_categories_name",
                table: "fragment_categories");

            migrationBuilder.RenameTable(
                name: "fragment_categories",
                newName: "fragment_category");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fragment_category",
                table: "fragment_category",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_ai_prompts_fragment_category_fragment_category_id",
                table: "ai_prompts",
                column: "fragment_category_id",
                principalTable: "fragment_category",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragment_category_fragment_category_id",
                table: "fragments",
                column: "fragment_category_id",
                principalTable: "fragment_category",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
