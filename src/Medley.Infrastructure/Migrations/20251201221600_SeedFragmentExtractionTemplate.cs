using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedFragmentExtractionTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var templateId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            var defaultPrompt = @"Analyze the following source content and extract meaningful knowledge fragments.

For each fragment you identify, provide:
- Title: A clear, descriptive title
- Summary: A brief summary (1-2 sentences)
- Category: Choose from [Decision, Action Item, Feature Request, User Feedback, Technical Discussion, Other]
- Content: The full extracted text";

            migrationBuilder.InsertData(
                table: "Templates",
                columns: new[] { "Id", "Name", "Type", "Content", "CreatedAt" },
                values: new object[] { templateId, "Fragment Extraction Prompt", "FragmentExtraction", defaultPrompt, now }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Templates",
                keyColumn: "Type",
                keyValue: "FragmentExtraction"
            );
        }
    }
}

