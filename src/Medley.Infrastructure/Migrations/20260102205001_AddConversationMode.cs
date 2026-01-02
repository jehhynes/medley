using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "mode",
                table: "chat_conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Seed the Article Chat template
            migrationBuilder.InsertData(
                table: "templates",
                columns: new[] { "id", "name", "type", "content", "description", "created_at", "last_modified_at" },
                values: new object[]
                {
                    Guid.NewGuid(),
                    "Article Chat",
                    5, // ArticleChat
                    @"You are an AI assistant helping users with the article ""{article.Title}"".
Help the user improve, expand, or answer questions about this article. Be concise and helpful.

------------------

Current Article Content:
{article.Content}
",
                    "Template for general chat mode on an article",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mode",
                table: "chat_conversations");
        }
    }
}
