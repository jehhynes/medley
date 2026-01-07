using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameContentToText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "text",
                table: "chat_messages",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("update chat_messages set text=content");

            migrationBuilder.DropColumn(
                name: "content",
                table: "chat_messages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "content",
                table: "chat_messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("update chat_messages set content=text");

            migrationBuilder.DropColumn(
                name: "text",
                table: "chat_messages");

        }
    }
}
