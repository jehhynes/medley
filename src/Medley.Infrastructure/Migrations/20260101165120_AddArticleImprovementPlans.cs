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
                    @"You are an AI assistant helping to improve the article.

Your task is to research and create a comprehensive improvement plan.

 1. Analyze the current article content
 2. Read the article type guidance to understand the type of content we want to include in the article.
 3. Identify gaps in knowledge or areas for improvement in the article content.
 4. Ask questions about the article that are within the scope of the article's subject and article type guidance, but are not answered by the article content.
 5. Use `SearchFragments `and `FindSimilarFragments `to find relevant knowledge. Make all desired tool calls for additional fragments in one request.
 6. Use `GetFragmentContent `to review most relevant fragments in detail. Make all desired tool calls for fragment content in one request.
 7. Analyze the fragments for value based on the article subject, article type guidance, and quality of the fragment.
 8. Separate fragments into 3 groups by the potential value they can add to the article:
    1. No value - omit entirely from plan.
    2. Minor or uncertain value - Add to plan but mark with `include: false`
    3. Major value - Add to plan with `include: true`
 9. Analyze the fragments that will be included in the plan, and develop a plan for using them to improve the article.
10. Use `CreatePlan` to generate your plan. Only call once. Do not retry if it fails.
    1. Be sure not to include duplicates of the same fragment in the plan.
11. In your summary message, do not repeat the plan, simply give a 1 to 2 sentence summary of what you did.
",
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
