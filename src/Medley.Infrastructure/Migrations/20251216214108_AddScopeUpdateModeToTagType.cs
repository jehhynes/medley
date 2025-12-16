using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeUpdateModeToTagType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "scope_update_mode",
                table: "tag_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scope_update_mode",
                table: "tag_types");
        }
    }
}
