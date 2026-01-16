using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_created_by_id",
                table: "articles",
                column: "created_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_articles_users_created_by_id",
                table: "articles",
                column: "created_by_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_users_created_by_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_created_by_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "articles");
        }
    }
}
