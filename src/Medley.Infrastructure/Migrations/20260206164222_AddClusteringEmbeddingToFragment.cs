using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClusteringEmbeddingToFragment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "clustering_embedding",
                table: "fragments",
                type: "vector(2000)",
                nullable: true);

            // Seed FragmentEmbeddingInstructions prompt
            migrationBuilder.InsertData(
                table: "ai_prompts",
                columns: new[] { "id", "type", "content", "created_at" },
                values: new object[]
                {
                    Guid.NewGuid(),
                    14, // FragmentEmbeddingInstructions
                    @"Embed the following fragment to represent ONLY its core technical subject matter.

Focus on:
- The system, feature, or domain involved
- The primary technical concept or failure mode

Ignore:
- Words like “troubleshooting”, “investigating”, “issue”, “problem”, “error” unless they describe a specific technical condition
- Procedural or support-related framing
- Generic debugging language

The embedding should cluster fragments by shared technical domain, not by task type.",
                    DateTimeOffset.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clustering_embedding",
                table: "fragments");


            // Remove the seeded prompt
            migrationBuilder.Sql("DELETE FROM ai_prompts WHERE type = 14");
        }
    }
}
