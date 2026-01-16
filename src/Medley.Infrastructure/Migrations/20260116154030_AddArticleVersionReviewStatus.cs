using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleVersionReviewStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_active",
                table: "article_versions");

            migrationBuilder.AddColumn<int>(
                name: "review_action",
                table: "article_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                table: "article_versions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_id",
                table: "article_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_reviewed_by_id",
                table: "article_versions",
                column: "reviewed_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_article_versions_users_reviewed_by_id",
                table: "article_versions",
                column: "reviewed_by_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_article_versions_users_reviewed_by_id",
                table: "article_versions");

            migrationBuilder.DropIndex(
                name: "ix_article_versions_reviewed_by_id",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "review_action",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "reviewed_by_id",
                table: "article_versions");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "article_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
