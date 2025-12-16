using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedConfidenceScoringTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var templateId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            var defaultContent = @"You are evaluating the confidence level of knowledge fragments that were previously extracted from meeting transcripts. Your task is to assess how strongly the original source content supports each extracted fragment.
## Input Format

You will receive a JSON payload containing:
- **sourceContent**: The complete original source text from which fragments were extracted
- **fragments**: An array of extracted fragments, each with:
  - **id**: Unique identifier for the fragment (GUID)
  - **category**: Content category (Tutorial, How-To, Concept, etc.)
  - **content**: The full extracted content

## Your Task

For each derived fragment, assign a confidence level on a scale of 0 to 4 that reflects how well the source content supports the claims, information, or guidance in that fragment. Return the fragment ID, confidence level, and a brief comment explaining your assessment.

## Evaluation Factors

- Explicitness of the statement (direct vs speculative)
- Speaker authority and role (Statements by support staff are more trusted than customer statements)
- Presence or absence of consensus
- Whether the statement was contradicted or revised later
- Level of specificity
- Whether the statement represents a decision vs open discussion

## Confidence Scale

- 4 (Certain): Explicit, concrete description with no contradictions, by a trusted speaker.
- 3 (High): Clear description but missing depth, edge cases, or consensus
- 2 (Medium): Implied or generalized information treated as true.
- 1 (Low): Anecdotal, speculative, or framed as personal belief.
- 0 (Unclear): Insufficient or conflicting information.
";

            var escapedContent = defaultContent.Replace("'", "''");

            migrationBuilder.Sql($@"
                INSERT INTO ""Templates"" (""Id"", ""Name"", ""Type"", ""Content"", ""Description"", ""CreatedAt"")
                SELECT '{templateId}', 'Confidence Scoring Prompt', 3,
                    '{escapedContent}',
                    'Instructions for assigning a confidence score to extracted fragments.',
                    '{now:O}'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Templates"" WHERE ""Type"" = 3
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Templates"" 
                WHERE ""Type"" = 3 AND ""Name"" = 'Confidence Scoring Prompt';
            ");
        }
    }
}
