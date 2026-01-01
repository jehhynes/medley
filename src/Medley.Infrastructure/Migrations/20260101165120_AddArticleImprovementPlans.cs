using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleImprovementPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    instructions = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_plans_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plans_chat_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_plans_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "plan_fragments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    similarity_score = table.Column<double>(type: "double precision", nullable: false),
                    include = table.Column<bool>(type: "boolean", nullable: false),
                    reasoning = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plan_fragments", x => x.id);
                    table.ForeignKey(
                        name: "fk_plan_fragments_fragments_fragment_id",
                        column: x => x.fragment_id,
                        principalTable: "fragments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_plan_fragments_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plan_fragments_fragment_id",
                table: "plan_fragments",
                column: "fragment_id");

            migrationBuilder.CreateIndex(
                name: "ix_plan_fragments_plan_id_fragment_id",
                table: "plan_fragments",
                columns: new[] { "plan_id", "fragment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_plans_article_id_status",
                table: "plans",
                columns: new[] { "article_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_plans_conversation_id",
                table: "plans",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_plans_created_by_user_id",
                table: "plans",
                column: "created_by_user_id");

            // Seed the Article Improvement Plan template
            migrationBuilder.InsertData(
                table: "templates",
                columns: new[] { "id", "name", "type", "content", "description", "created_at", "last_modified_at" },
                values: new object[]
                {
                    Guid.NewGuid(),
                    "Article Improvement Plan",
                    4, // ArticleImprovementPlan
                    @"You are an AI assistant helping to improve the article ""{article.Title}"".

Your task is to research and create a comprehensive improvement plan by:
1. Analyzing the current article content
2. Using search_fragments to find relevant knowledge
3. Using find_similar_to_article to discover related content
4. Using get_fragment_content to review promising fragments in detail
5. Using create_plan to generate structured recommendations

For each recommended fragment, provide:
- Whether to include it in the article (true/false - no optional state)
- Detailed reasoning for your recommendation
- Specific instructions combining placement and usage guidance

Create a diverse set of recommendations covering different aspects:
- Technical details and implementation examples (typically include=true)
- Context and background information (typically include=true)
- Related decisions and rationale (typically include=true)
- Tangential or less relevant content (include=false, for reference)

Your instructions should be in markdown format and include:
- Overview of recommended improvements
- Specific sections to add or enhance
- Writing guidelines for applying the fragments

Be thorough but selective - quality over quantity.",
                    "Template for generating article improvement plans with fragment recommendations",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_fragments");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}
