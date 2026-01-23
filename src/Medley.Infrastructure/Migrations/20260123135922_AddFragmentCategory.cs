using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFragmentCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create fragment_category table
            migrationBuilder.CreateTable(
                name: "fragment_category",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fragment_category", x => x.id);
                });

            // Step 2: Seed default fragment categories
            var now = DateTimeOffset.UtcNow;
            var tutorialId = Guid.NewGuid();
            var howToId = Guid.NewGuid();
            var conceptId = Guid.NewGuid();
            var bestPracticeId = Guid.NewGuid();
            var troubleshootingId = Guid.NewGuid();
            var useCaseId = Guid.NewGuid();
            var faqId = Guid.NewGuid();

            migrationBuilder.InsertData(
                table: "fragment_category",
                columns: new[] { "id", "name", "icon", "created_at" },
                values: new object[,]
                {
                    { tutorialId, "Tutorial", "bi-play-circle", now },
                    { howToId, "How-To", "bi-card-checklist", now },
                    { conceptId, "Concept", "bi-lightbulb", now },
                    { bestPracticeId, "Best Practice", "bi-shield-check", now },
                    { troubleshootingId, "Troubleshooting", "bi-wrench", now },
                    { useCaseId, "Use Case", "bi-briefcase", now },
                    { faqId, "FAQ", "fa-comment-question", now }
                });

            // Step 3: Add fragment_category_id column (nullable initially for data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "fragment_category_id",
                table: "fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fragment_category_id",
                table: "ai_prompts",
                type: "uuid",
                nullable: true);

            // Step 4: Migrate existing fragment data (map category strings to IDs)
            // Map common variations to categories, default unmapped to How-To
            migrationBuilder.Sql($@"
                UPDATE fragments 
                SET fragment_category_id = '{tutorialId}'
                WHERE LOWER(category) IN ('tutorial', 'tutorials');
                
                UPDATE fragments 
                SET fragment_category_id = '{howToId}'
                WHERE LOWER(category) IN ('how-to', 'how to', 'howto');
                
                UPDATE fragments 
                SET fragment_category_id = '{conceptId}'
                WHERE LOWER(category) IN ('concept', 'concepts');
                
                UPDATE fragments 
                SET fragment_category_id = '{bestPracticeId}'
                WHERE LOWER(category) IN ('best practice', 'best practices', 'bestpractice');
                
                UPDATE fragments 
                SET fragment_category_id = '{troubleshootingId}'
                WHERE LOWER(category) IN ('troubleshooting');
                
                UPDATE fragments 
                SET fragment_category_id = '{useCaseId}'
                WHERE LOWER(category) IN ('use case', 'usecase', 'use cases');
                
                -- Map all remaining unmapped fragments to How-To (default fallback)
                UPDATE fragments 
                SET fragment_category_id = '{howToId}'
                WHERE fragment_category_id IS NULL;
            ");

            // Step 5: Make fragment_category_id required (NOT NULL)
            migrationBuilder.AlterColumn<Guid>(
                name: "fragment_category_id",
                table: "fragments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Step 6: Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_fragments_fragment_category_id",
                table: "fragments",
                column: "fragment_category_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_prompts_fragment_category_id",
                table: "ai_prompts",
                column: "fragment_category_id");

            // Step 7: Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "fk_ai_prompts_fragment_category_fragment_category_id",
                table: "ai_prompts",
                column: "fragment_category_id",
                principalTable: "fragment_category",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragment_category_fragment_category_id",
                table: "fragments",
                column: "fragment_category_id",
                principalTable: "fragment_category",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Step 8: Drop old category column
            migrationBuilder.DropColumn(
                name: "category",
                table: "fragments");

            foreach(var x in new[]
            {
                new { Content = "Learning-oriented guide that provides hands-on experience to help beginners gain practical understanding of a product or feature through a carefully managed path from start to finish", CategoryId = tutorialId },
                new { Content = "Task-oriented guide that takes users through a series of sequential steps to solve a specific real-world problem or accomplish a goal", CategoryId = howToId },
                new { Content = "Also known as \"explanation\", concept articles are understanding-oriented documentation that explains the \"why\" and \"what\" behind a topic by providing foundational knowledge, context, background information, connections to other concepts, and use cases—helping readers build a deeper understanding without including step-by-step instructions or reference material.", CategoryId = conceptId },
                new { Content = "Guidance that outlines the most effective, reliable, and recommended ways to use the product or feature. Best practices articles help users avoid common pitfalls, improve efficiency, and achieve optimal outcomes by following proven strategies or workflows.", CategoryId = bestPracticeId },
                new { Content = "Helps users resolve specific problems by describing the symptoms users experience, explaining the root causes of those symptoms, and providing step-by-step solutions or workarounds to fix each issue.", CategoryId = troubleshootingId },
                new { Content = "Describes real-world scenarios that show how a product or feature is used to achieve a specific outcome, highlighting the context, actors, and value delivered rather than step-by-step instructions.", CategoryId = useCaseId },
                new { Content = "List questions the customer asked (along with the answer), including recurring pain points or confusion areas. Document clear, concise answers to common questions.", CategoryId = faqId }
            })
            {
                migrationBuilder.InsertData(
                  table: "ai_prompts",
                  columns: new[] { "id", "type", "content", "fragment_category_id", "created_at" },
                  values: new object[,]
                  {
                     { Guid.NewGuid(), 9, x.Content, x.CategoryId, now},
                  });
            }

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException();
            migrationBuilder.DropForeignKey(
                name: "fk_ai_prompts_fragment_category_fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragment_category_fragment_category_id",
                table: "fragments");

            migrationBuilder.DropTable(
                name: "fragment_category");

            migrationBuilder.DropIndex(
                name: "ix_fragments_fragment_category_id",
                table: "fragments");

            migrationBuilder.DropIndex(
                name: "ix_ai_prompts_fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.DropColumn(
                name: "fragment_category_id",
                table: "fragments");

            migrationBuilder.DropColumn(
                name: "fragment_category_id",
                table: "ai_prompts");

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "fragments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
