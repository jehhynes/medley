using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedKnowledgeUnitClusteringPrompt : Migration
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
                    13, // KnowledgeUnitClustering
                    @"**Your task is to cluster fragments of meeting content into knowledge units that represent the same or highly similar ideas.**

### **Core Rules**

- Each knowledge unit represents a **single coherent idea, concept, or guidance**
- Fragments **may appear in multiple knowledge units** if they support multiple distinct ideas
- Include only fragments that clearly convey the same or highly similar idea
- Ignore unrelated or tangentially related fragments
- Avoid duplicate knowledge units that differ only trivially

### **Scope Rules**

- Each fragment is designated Internal (employee) or External (customer)
- Group different scopes together **only if** content is very similar AND perspective/audience are the same
- **Do not mix employee-facing and customer-facing content**

### **Confidence Resolution**

- **Confidence**: Use the **highest confidence level** among included fragments

  - Demote if content is unsupported, contradicted by higher-trust speakers, or speculative
  - Follow fragment weighting instructions

- **ConfidenceComment**: Explain the assigned confidence, including:

  - Rationale from original fragments
  - Logic applied during clustering
  - How conflicts or disagreements were resolved

### **Canonical Content Creation**

- **Synthesize** fragments into a single distilled statement that captures the core insight
- **Target length**: 100-400 words per knowledge unit
- **Prioritize**: Actionable guidance &gt; observations &gt; context
- **Remove**: Examples, anecdotes, repetition, hedging language, attribution phrases
- **Keep only**: Core claim + essential supporting facts
- Exclude ideas if contradicted by higher-trust/later sources or they fail fragment weighting guidance

### Ignored Fragments

- Every fragment ID must *either* be included in one or more generated Knowledge Units, *or* be included in the list of ignored fragments.
- Include a rationale for each ignored fragment, why it was omitted.",
                    null,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM ai_prompts WHERE type = 13;");
        }
    }
}
