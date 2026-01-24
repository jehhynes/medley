using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleFragmentsJoinTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_fragments_articles_article_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_fragments_article_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "article_id",
                table: "fragments");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_fragments");

            migrationBuilder.AddColumn<Guid>(
                name: "article_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_fragments_article_id",
                table: "fragments",
                column: "article_id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_articles_article_id",
                table: "fragments",
                column: "article_id",
                principalTable: "articles",
                principalColumn: "id");
        }
    }
}
