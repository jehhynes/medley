using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfidenceToFragment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Confidence",
                table: "Fragments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfidenceComment",
                table: "Fragments",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "ConfidenceComment",
                table: "Fragments");
        }
    }
}
