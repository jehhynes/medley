using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChatConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "change_message",
                table: "article_versions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "article_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_version_id",
                table: "article_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version_type",
                table: "article_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_conversations_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_conversations_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_type = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_messages_chat_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_chat_messages_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_parent_version_id",
                table: "article_versions",
                column: "parent_version_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_versions_version_type",
                table: "article_versions",
                column: "version_type");

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversations_article_id_state",
                table: "chat_conversations",
                columns: new[] { "article_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversations_created_at",
                table: "chat_conversations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversations_created_by_user_id",
                table: "chat_conversations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_conversation_id_created_at",
                table: "chat_messages",
                columns: new[] { "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_user_id",
                table: "chat_messages",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_article_versions_article_versions_parent_version_id",
                table: "article_versions",
                column: "parent_version_id",
                principalTable: "article_versions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_article_versions_article_versions_parent_version_id",
                table: "article_versions");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            migrationBuilder.DropIndex(
                name: "ix_article_versions_parent_version_id",
                table: "article_versions");

            migrationBuilder.DropIndex(
                name: "ix_article_versions_version_type",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "change_message",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "parent_version_id",
                table: "article_versions");

            migrationBuilder.DropColumn(
                name: "version_type",
                table: "article_versions");
        }
    }
}
