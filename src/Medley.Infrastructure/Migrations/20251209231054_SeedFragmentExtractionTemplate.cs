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
4. Organize the extracted information according to the following categories: Tutorial, How-To, Concept, Best Practice, Troubleshooting, FAQ

## Content Categories

### 1. Tutorial
- Learning-oriented guide that provides hands-on experience to help beginners gain practical understanding of a product or feature through a carefully managed path from start to finish

### 2. How-To
- Task-oriented guide that takes users through a series of sequential steps to solve a specific real-world problem or accomplish a goal

### 3. Concept
- Also known as ""explanation"", concept articles are understanding-oriented documentation that explains the ""why"" and ""what"" behind a topic by providing foundational knowledge, context, background information, connections to other concepts, and use cases—helping readers build a deeper understanding without including step-by-step instructions or reference material.

### 4. Best Practice
- Guidance that outlines the most effective, reliable, and recommended ways to use the product or feature. Best practices articles help users avoid common pitfalls, improve efficiency, and achieve optimal outcomes by following proven strategies or workflows.

### 5. Troubleshooting
- Helps users resolve specific problems by describing the symptoms users experience, explaining the root causes of those symptoms, and providing step-by-step solutions or workarounds to fix each issue.

### 6. FAQ
- List questions the customer asked (along with the answer).
- Recurring pain points or confusion areas.
- Document clear, concise answers to common questions.

## Authoring Guidelines

### Content Focus
- Third-party systems and integrations
- Best practices and expert recommendations
- Edge cases, lesser-known features and capabilities, power-user workflows
- Connections between different features or areas in the system
- Not just _what_ features exist, but more importantly, _why_ the system works the way it does, and _how_ to best use it.
- Alternative approaches and when to choose which approach.
- Only include current functionality, ignore any mention of proposed/future features.

### Format Requirements
- Use clear, concise language suitable for end users. Use full sentences to reduce ambiguity.
- Create searchable titles for each entry
- Focus exclusively on user-facing workflows and experiences.
- Do not include content that is specific to a single client, their specific onboarding process, etc.
- Use bulleted and numbered lists sparingly - prefer prose.
- Include all UI elements mentioned (Screens, Menus, Buttons, Form fields, etc)

### Voice & Tone
* **Friendly but professional.** Write as a helpful teammate: clear, confident, never condescending.
* **Use plain language.** Prefer simple words (""use"" over ""utilize""), avoid jargon unless the audience expects it.
* **Active voice.** ""Click Save"" not ""The Save button should be clicked"".
* **Second person.** Address the reader as *you*; refer to the company or system as *we* when needed.
* **Positive framing.** Emphasize what the user *can* do rather than what they cannot.
* **Consistent terminology.** Always use the product’s official names for menus, modules, and settings.";

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
