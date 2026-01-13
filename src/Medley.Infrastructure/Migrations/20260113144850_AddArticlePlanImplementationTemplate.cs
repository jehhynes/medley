using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlePlanImplementationTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed ArticlePlanImplementation template
            migrationBuilder.InsertData(
                table: "templates",
                columns: new[] { "id", "name", "type", "content", "description", "created_at", "last_modified_at" },
                values: new object[]
                {
                    Guid.NewGuid(),
                    "Article Plan Implementation",
                    6, // ArticlePlanImplementation
                    @"# Article Improvement Plan Implementation

You are an AI assistant tasked with implementing an article improvement plan. Your goal is to enhance the article by incorporating the recommended fragments according to the specific instructions provided.

## Your Task

1. **Review the Plan Instructions**: Carefully read the overall improvement plan instructions below.
2. **Analyze Current Article**: Understand the current state of the article, including its title, summary, and content.
3. **Incorporate Fragments**: For each included fragment, follow the specific instructions on how to integrate it into the article.
4. **Maintain Quality**: Ensure the improved article:
   - Has a clear, logical flow
   - Maintains consistent tone and style
   - Properly integrates all included fragments
   - Is well-structured with appropriate headings and sections
   - Preserves important existing content while enhancing it

5. **Generate Improved Content**: Create the complete improved article content.
6. **Document Changes**: Write a clear, concise change message describing what was improved.

## Important Guidelines

- Follow the fragment-specific instructions carefully
- Maintain the article's original purpose and scope
- Ensure smooth transitions between sections
- Preserve factual accuracy from both the original article and fragments
- Use markdown formatting appropriately

## Final Step

Once you have created the improved article content, call the `CreateArticleVersion` tool with:
- The complete improved article content
- A descriptive change message summarizing the improvements made

The article context, plan instructions, and included fragments are provided below.",
                    "Template for implementing article improvement plans with AI assistance",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the seeded template
            migrationBuilder.Sql("DELETE FROM templates WHERE type = 6");
        }
    }
}
