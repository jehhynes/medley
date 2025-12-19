using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "article_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content_snapshot = table.Column<string>(type: "text", nullable: false),
                    content_diff = table.Column<string>(type: "text", nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_versions", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_versions_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_versions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_article_id",
                table: "article_versions",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_article_id_created_at",
                table: "article_versions",
                columns: new[] { "article_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_article_id_version_number",
                table: "article_versions",
                columns: new[] { "article_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_created_by",
                table: "article_versions",
                column: "created_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_versions");
        }
    }
}
