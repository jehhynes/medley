using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClusteringMessageToFragment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "clustering_message",
                table: "fragments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clustering_message",
                table: "fragments");
        }
    }
}
