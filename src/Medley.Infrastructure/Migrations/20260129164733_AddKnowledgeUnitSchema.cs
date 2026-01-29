using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeUnitSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create knowledge_units table
            migrationBuilder.CreateTable(
                name: "knowledge_units",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    confidence = table.Column<int>(type: "integer", nullable: false),
                    confidence_comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    clustering_comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fragment_category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    embedding = table.Column<Vector>(type: "vector(2000)", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_knowledge_units_fragment_categories_fragment_category_id",
                        column: x => x.fragment_category_id,
                        principalTable: "fragment_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Create indexes on knowledge_units table
            migrationBuilder.CreateIndex(
                name: "ix_knowledge_units_fragment_category_id",
                table: "knowledge_units",
                column: "fragment_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_knowledge_units_created_at",
                table: "knowledge_units",
                column: "created_at");

            // Step 3: Create vector index on knowledge_units.embedding for similarity search
            migrationBuilder.Sql(
                "CREATE INDEX ix_knowledge_units_embedding ON knowledge_units USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");

            // Step 4: Create article_knowledge_units join table
            migrationBuilder.CreateTable(
                name: "article_knowledge_units",
                columns: table => new
                {
                    articles_id = table.Column<Guid>(type: "uuid", nullable: false),
                    knowledge_units_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_knowledge_units", x => new { x.articles_id, x.knowledge_units_id });
                    table.ForeignKey(
                        name: "fk_article_knowledge_units_articles_articles_id",
                        column: x => x.articles_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_knowledge_units_knowledge_units_knowledge_units_id",
                        column: x => x.knowledge_units_id,
                        principalTable: "knowledge_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_knowledge_units_knowledge_units_id",
                table: "article_knowledge_units",
                column: "knowledge_units_id");

            // Step 5: Add knowledge_unit_id column to fragments table
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

            // Step 6: Remove old clustering columns from fragments table
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragments_clustered_into_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragments_representative_fragment_id",
                table: "fragments");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "ix_fragments_clustered_into_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_fragments_representative_fragment_id",
                table: "fragments");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "is_cluster",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "clustered_into_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "representative_fragment_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "clustering_message",
                table: "fragments");

            // Step 7: Drop article_fragments join table
            migrationBuilder.DropTable(
                name: "article_fragments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Recreate article_fragments join table
            migrationBuilder.CreateTable(
                name: "article_fragments",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragments_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_fragments", x => new { x.article_id, x.fragments_id });
                    table.ForeignKey(
                        name: "fk_article_fragments_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_fragments_fragments_fragments_id",
                        column: x => x.fragments_id,
                        principalTable: "fragments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_fragments_fragments_id",
                table: "article_fragments",
                column: "fragments_id");

            // Step 2: Restore old clustering columns to fragments table
            migrationBuilder.AddColumn<bool>(
                name: "is_cluster",
                table: "fragments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "clustered_into_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "representative_fragment_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clustering_message",
                table: "fragments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            // Step 3: Create indexes for restored columns
            migrationBuilder.CreateIndex(
                name: "ix_fragments_clustered_into_id",
                table: "fragments",
                column: "clustered_into_id");

            migrationBuilder.CreateIndex(
                name: "ix_fragments_representative_fragment_id",
                table: "fragments",
                column: "representative_fragment_id");

            // Step 4: Restore foreign keys
            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragments_clustered_into_id",
                table: "fragments",
                column: "clustered_into_id",
                principalTable: "fragments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragments_representative_fragment_id",
                table: "fragments",
                column: "representative_fragment_id",
                principalTable: "fragments",
                principalColumn: "id");

            // Step 5: Drop knowledge_unit_id from fragments
            migrationBuilder.DropForeignKey(
                name: "fk_fragments_knowledge_units_knowledge_unit_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_fragments_knowledge_unit_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "knowledge_unit_id",
                table: "fragments");

            // Step 6: Drop article_knowledge_units join table
            migrationBuilder.DropTable(
                name: "article_knowledge_units");

            // Step 7: Drop vector index on knowledge_units.embedding
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_knowledge_units_embedding;");

            // Step 8: Drop knowledge_units table
            migrationBuilder.DropTable(
                name: "knowledge_units");
        }
    }
}
