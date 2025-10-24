using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyAndBaseUrlToIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfigurationJson",
                table: "Integrations");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Integrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "Integrations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "Integrations");

            migrationBuilder.AddColumn<string>(
                name: "ConfigurationJson",
                table: "Integrations",
                type: "text",
                nullable: true);
        }
    }
}
