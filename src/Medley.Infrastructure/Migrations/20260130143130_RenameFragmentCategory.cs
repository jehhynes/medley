using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameFragmentCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "fk_ai_prompts_fragment_categories_fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragment_categories_fragment_category_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_knowledge_units_fragment_categories_fragment_category_id",
                table: "knowledge_units");

            // Rename the table
            migrationBuilder.RenameTable(
                name: "fragment_categories",
                newName: "knowledge_categories");

            // Rename the primary key constraint
            migrationBuilder.RenameIndex(
                name: "pk_fragment_categories",
                table: "knowledge_categories",
                newName: "pk_knowledge_categories");

            // Rename the unique index
            migrationBuilder.RenameIndex(
                name: "ix_fragment_categories_name",
                table: "knowledge_categories",
                newName: "ix_knowledge_categories_name");

            // Rename foreign key columns in referencing tables
            migrationBuilder.RenameColumn(
                name: "fragment_category_id",
                table: "knowledge_units",
                newName: "knowledge_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_knowledge_units_fragment_category_id",
                table: "knowledge_units",
                newName: "ix_knowledge_units_knowledge_category_id");

            migrationBuilder.RenameColumn(
                name: "fragment_category_id",
                table: "fragments",
                newName: "knowledge_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_fragments_fragment_category_id",
                table: "fragments",
                newName: "ix_fragments_knowledge_category_id");

            migrationBuilder.RenameColumn(
                name: "fragment_category_id",
                table: "ai_prompts",
                newName: "knowledge_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_ai_prompts_fragment_category_id",
                table: "ai_prompts",
                newName: "ix_ai_prompts_knowledge_category_id");

            // Recreate foreign key constraints with new names
            migrationBuilder.AddForeignKey(
                name: "fk_ai_prompts_knowledge_categories_knowledge_category_id",
                table: "ai_prompts",
                column: "knowledge_category_id",
                principalTable: "knowledge_categories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_knowledge_categories_knowledge_category_id",
                table: "fragments",
                column: "knowledge_category_id",
                principalTable: "knowledge_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_knowledge_units_knowledge_categories_knowledge_category_id",
                table: "knowledge_units",
                column: "knowledge_category_id",
                principalTable: "knowledge_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "fk_ai_prompts_knowledge_categories_knowledge_category_id",
                table: "ai_prompts");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_knowledge_categories_knowledge_category_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_knowledge_units_knowledge_categories_knowledge_category_id",
                table: "knowledge_units");

            // Rename foreign key columns back in referencing tables
            migrationBuilder.RenameColumn(
                name: "knowledge_category_id",
                table: "knowledge_units",
                newName: "fragment_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_knowledge_units_knowledge_category_id",
                table: "knowledge_units",
                newName: "ix_knowledge_units_fragment_category_id");

            migrationBuilder.RenameColumn(
                name: "knowledge_category_id",
                table: "fragments",
                newName: "fragment_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_fragments_knowledge_category_id",
                table: "fragments",
                newName: "ix_fragments_fragment_category_id");

            migrationBuilder.RenameColumn(
                name: "knowledge_category_id",
                table: "ai_prompts",
                newName: "fragment_category_id");

            migrationBuilder.RenameIndex(
                name: "ix_ai_prompts_knowledge_category_id",
                table: "ai_prompts",
                newName: "ix_ai_prompts_fragment_category_id");

            // Rename the unique index back
            migrationBuilder.RenameIndex(
                name: "ix_knowledge_categories_name",
                table: "knowledge_categories",
                newName: "ix_fragment_categories_name");

            // Rename the primary key constraint back
            migrationBuilder.RenameIndex(
                name: "pk_knowledge_categories",
                table: "knowledge_categories",
                newName: "pk_fragment_categories");

            // Rename the table back
            migrationBuilder.RenameTable(
                name: "knowledge_categories",
                newName: "fragment_categories");

            // Recreate foreign key constraints with original names
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

            migrationBuilder.AddForeignKey(
                name: "fk_knowledge_units_fragment_categories_fragment_category_id",
                table: "knowledge_units",
                column: "fragment_category_id",
                principalTable: "fragment_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
