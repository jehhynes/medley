using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentArticleId",
                table: "Articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ParentArticleId",
                table: "Articles",
                column: "ParentArticleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Articles_ParentArticleId",
                table: "Articles",
                column: "ParentArticleId",
                principalTable: "Articles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Articles_ParentArticleId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ParentArticleId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ParentArticleId",
                table: "Articles");
        }
    }
}
