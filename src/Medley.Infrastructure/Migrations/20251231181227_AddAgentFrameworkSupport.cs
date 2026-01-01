using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentFrameworkSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "message_type",
                table: "chat_messages");

            migrationBuilder.AddColumn<int>(
                name: "role",
                table: "chat_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "serialized_message",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "chat_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "role",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "serialized_message",
                table: "chat_messages");

            migrationBuilder.AddColumn<int>(
                name: "message_type",
                table: "chat_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
