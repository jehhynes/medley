using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationRunningTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_running",
                table: "chat_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "current_conversation_id",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_current_conversation_id",
                table: "articles",
                column: "current_conversation_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_articles_chat_conversations_current_conversation_id",
                table: "articles",
                column: "current_conversation_id",
                principalTable: "chat_conversations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_chat_conversations_current_conversation_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_current_conversation_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "is_running",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "current_conversation_id",
                table: "articles");
        }
    }
}
