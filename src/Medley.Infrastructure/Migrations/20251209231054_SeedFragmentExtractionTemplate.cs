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
            var defaultContent = @"You are tasked with analyzing customer support transcripts to create a comprehensive knowledge base. Process the provided transcript file and extract information into the following structured categories.

## Process

1. Arrange the content using primarily prose and complete sentences, ensuring that all technical terms, instructions, guidance, and any other information relevant to a knowledge base help article are preserved.
2. Group the content logically by topic, with each piece appropriate for inclusion in a single help article.
3. Eliminate duplication to ensure the same information does not appear in multiple fragments.
4. Organize the extracted information according to the provided categories.

## Authoring Guidelines

### Content Focus

- Third-party systems and integrations
- Best practices and expert recommendations
- Edge cases, lesser-known features and capabilities, power-user workflows
- Connections between different features or areas in the system
- Not just *what* features exist, but more importantly, *why* the system works the way it does, and *how* to best use it.
- Alternative approaches and when to choose which approach.
- Only include current functionality, ignore any mention of proposed/future features or customer's legacy systems.

### Format Requirements

- Use clear, concise language suitable for end users. Use full sentences to reduce ambiguity.
- Create searchable titles for each entry
- Focus exclusively on user-facing workflows and experiences.
- Do not include content that is specific to a single client, their specific onboarding process, etc.
- Use bulleted and numbered lists sparingly - prefer prose.
- Include all UI elements mentioned (Screens, Menus, Buttons, Form fields, etc)
- Each fragment's length should be determined by the detail in source material, not a formatting decision. Do not attempt to normalize fragment length.

### Voice & Tone

- **Friendly but professional.** Write as a helpful teammate: clear, confident, never condescending.
- **Use plain language.** Prefer simple words (""use"" over ""utilize""), avoid jargon unless the audience expects it.
- **Active voice.** ""Click Save"" not ""The Save button should be clicked"".
- **Second person.** Address the reader as *you*; refer to the company or system as *we* when needed.
- **Positive framing.** Emphasize what the user *can* do rather than what they cannot.
- **Consistent terminology.** Always use the product’s official names for menus, modules, and settings.";

            var escapedContent = defaultContent.Replace("'", "''");

            migrationBuilder.Sql($@"
                INSERT INTO ""Templates"" (""Id"", ""Name"", ""Type"", ""Content"", ""Description"", ""CreatedAt"")
                SELECT '{templateId}', 'Fragment Extraction Prompt', 1,
                    '{escapedContent}',
                    'Instructions for extracting knowledge fragments from source content',
                    '{now:O}'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Templates"" WHERE ""Type"" = 1
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Templates"" 
                WHERE ""Type"" = 1 AND ""Name"" = 'Fragment Extraction Prompt';
            ");
        }
    }
}
