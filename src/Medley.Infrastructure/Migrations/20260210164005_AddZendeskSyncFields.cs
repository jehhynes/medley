using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddZendeskSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "enable_article_zendesk_sync",
                table: "organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_synced_to_zendesk_at",
                table: "articles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "zendesk_article_id",
                table: "articles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enable_article_zendesk_sync",
                table: "organizations");

            migrationBuilder.DropColumn(
                name: "last_synced_to_zendesk_at",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "zendesk_article_id",
                table: "articles");
        }
    }
}
