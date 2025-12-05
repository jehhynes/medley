using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsInternalAndEmailDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInternal",
                table: "Sources",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDomain",
                table: "Organizations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInternal",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "EmailDomain",
                table: "Organizations");
        }
    }
}
