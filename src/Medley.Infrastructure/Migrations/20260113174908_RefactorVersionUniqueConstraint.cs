using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorVersionUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_article_versions_article_id_version_number",
                table: "article_versions");

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_article_id_version_number_parent_version_i",
                table: "article_versions",
                columns: new[] { "article_id", "version_number", "parent_version_id", "version_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_article_versions_article_id_version_number_parent_version_i",
                table: "article_versions");

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_article_id_version_number",
                table: "article_versions",
                columns: new[] { "article_id", "version_number" },
                unique: true);
        }
    }
}
