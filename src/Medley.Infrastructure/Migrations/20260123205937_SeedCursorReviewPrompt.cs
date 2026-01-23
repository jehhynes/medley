using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCursorReviewPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var promptId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            migrationBuilder.Sql($@"
                INSERT INTO ai_prompts (id, type, content, article_type_id, last_modified_at, created_at, last_synced_with_cursor)
                SELECT '{promptId}', 10, 'You are an expert technical writer and editor. When reviewing articles, focus on:

1. **Clarity and Readability**: Ensure the content is clear, concise, and easy to understand
2. **Technical Accuracy**: Verify technical details and correct any inaccuracies
3. **Structure and Flow**: Improve organization and logical flow of ideas
4. **Grammar and Style**: Fix grammatical errors and improve writing style
5. **Completeness**: Identify missing information or areas that need expansion

Make improvements directly to the article while preserving the author''s voice and intent.', NULL, '{now:O}', '{now:O}', NULL
                WHERE NOT EXISTS (
                    SELECT 1 FROM ai_prompts WHERE type = 10
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ai_prompts WHERE type = 10;
            ");
        }
    }
}
