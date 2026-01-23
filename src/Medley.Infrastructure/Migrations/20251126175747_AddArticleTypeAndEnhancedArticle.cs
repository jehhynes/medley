using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleTypeAndEnhancedArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ArticleTypeId",
                table: "Articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Articles",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ArticleTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ArticleTypeId",
                table: "Articles",
                column: "ArticleTypeId");

            migrationBuilder.AddForeignKey(
                 name: "FK_Articles_ArticleTypes_ArticleTypeId",
                 table: "Articles",
                 column: "ArticleTypeId",
                 principalTable: "ArticleTypes",
                 principalColumn: "Id");

            // Seed default ArticleTypes
            var now = DateTimeOffset.UtcNow;
            migrationBuilder.InsertData(
                table: "ArticleTypes",
                columns: new[] { "Id", "Name", "Icon", "CreatedAt" },
                values: new object[,]
                {
                    { Guid.NewGuid(), "Index", "bi-folder", now },
                    { Guid.NewGuid(), "How-To", "bi-card-checklist", now },
                    { Guid.NewGuid(), "Tutorial", "bi-play-circle", now },
                    { Guid.NewGuid(), "Reference", "bi-book", now },
                    { Guid.NewGuid(), "Concept", "bi-lightbulb", now },
                    { Guid.NewGuid(), "FAQ", "fa-comment-question", now },
                    { Guid.NewGuid(), "Troubleshooting", "bi-wrench", now }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_ArticleTypes_ArticleTypeId",
                table: "Articles");

            migrationBuilder.DropTable(
                name: "ArticleTypes");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ArticleTypeId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ArticleTypeId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Articles");
        }
    }
}
