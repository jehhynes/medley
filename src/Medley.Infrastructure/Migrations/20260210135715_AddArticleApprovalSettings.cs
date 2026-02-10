using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleApprovalSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "auto_approve",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "required_review_count",
                table: "organizations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "required_reviewer_id",
                table: "organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "review_successor_id",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "article_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_article_reviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_article_reviews_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_article_reviews_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_required_reviewer_id",
                table: "organizations",
                column: "required_reviewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_review_successor_id",
                table: "AspNetUsers",
                column: "review_successor_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_reviews_article_id",
                table: "article_reviews",
                column: "article_id");

            migrationBuilder.CreateIndex(
                name: "ix_article_reviews_reviewed_by_id",
                table: "article_reviews",
                column: "reviewed_by_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_asp_net_users_review_successor_id",
                table: "AspNetUsers",
                column: "review_successor_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_organizations_users_required_reviewer_id",
                table: "organizations",
                column: "required_reviewer_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_asp_net_users_review_successor_id",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "fk_organizations_users_required_reviewer_id",
                table: "organizations");

            migrationBuilder.DropTable(
                name: "article_reviews");

            migrationBuilder.DropIndex(
                name: "ix_organizations_required_reviewer_id",
                table: "organizations");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_review_successor_id",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "auto_approve",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "required_review_count",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "required_reviewer_id",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "review_successor_id",
                table: "AspNetUsers");
        }
    }
}
