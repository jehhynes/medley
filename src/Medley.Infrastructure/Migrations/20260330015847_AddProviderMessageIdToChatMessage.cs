using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderMessageIdToChatMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "provider_message_id",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_provider_message_id",
                table: "chat_messages",
                column: "provider_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_chat_messages_provider_message_id",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "provider_message_id",
                table: "chat_messages");
        }
    }
}
