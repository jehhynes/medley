using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentVersionToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_version_id",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_current_version_id",
                table: "articles",
                column: "current_version_id");

            migrationBuilder.AddForeignKey(
                name: "fk_articles_article_versions_current_version_id",
                table: "articles",
                column: "current_version_id",
                principalTable: "article_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Data migration: Set current_version_id to the latest User version for each article
            migrationBuilder.Sql(@"
                UPDATE articles
                SET current_version_id = (
                    SELECT id
                    FROM article_versions
                    WHERE article_versions.article_id = articles.id
                      AND article_versions.version_type = 0  -- VersionType.User
                    ORDER BY article_versions.version_number DESC
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM article_versions
                    WHERE article_versions.article_id = articles.id
                      AND article_versions.version_type = 0
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_article_versions_current_version_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_current_version_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "current_version_id",
                table: "articles");
        }
    }
}
