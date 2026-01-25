using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentPlanToArticle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_plan_id",
                table: "articles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_articles_current_plan_id",
                table: "articles",
                column: "current_plan_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_articles_plans_current_plan_id",
                table: "articles",
                column: "current_plan_id",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Data migration: Set current_plan_id to the active plan (Draft or InProgress) for each article
            migrationBuilder.Sql(@"
                UPDATE articles
                SET current_plan_id = (
                    SELECT id
                    FROM plans
                    WHERE plans.article_id = articles.id
                      AND plans.status IN (0, 1)  -- PlanStatus.Draft = 0, PlanStatus.InProgress = 1
                    ORDER BY plans.created_at DESC
                    LIMIT 1
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM plans
                    WHERE plans.article_id = articles.id
                      AND plans.status IN (0, 1)
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_plans_current_plan_id",
                table: "articles");

            migrationBuilder.DropIndex(
                name: "ix_articles_current_plan_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "current_plan_id",
                table: "articles");
        }
    }
}
