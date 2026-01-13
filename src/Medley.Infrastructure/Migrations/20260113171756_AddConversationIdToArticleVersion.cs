using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationIdToArticleVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "conversation_id",
                table: "article_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_conversation_id",
                table: "article_versions",
                column: "conversation_id");

            migrationBuilder.AddForeignKey(
                name: "fk_article_versions_chat_conversations_conversation_id",
                table: "article_versions",
                column: "conversation_id",
                principalTable: "chat_conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_article_versions_chat_conversations_conversation_id",
                table: "article_versions");

            migrationBuilder.DropIndex(
                name: "ix_article_versions_conversation_id",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "conversation_id",
                table: "article_versions");
        }
    }
}
