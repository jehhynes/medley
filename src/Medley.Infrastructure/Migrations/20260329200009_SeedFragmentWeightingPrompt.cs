using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedFragmentWeightingPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ai_prompts",
                columns: new[] { "id", "type", "content", "article_type_id", "last_modified_at", "created_at", "last_synced_with_cursor" },
                values: new object[]
                {
                    Guid.NewGuid(),
                    12, // FragmentWeighting
                    @"Consider the following weighting factors:

1. Higher confidence fragments take priority if content conflicts
2. More recent fragments take priority if content conflicts
3. Fragments from highly trusted speakers carry more weight
4. External meetings should take higher priority over internal if by a trusted speaker, because internal meetings are more likely to be explorative or hypothetical.",
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM ai_prompts WHERE type = 12;");
        }
    }
}
