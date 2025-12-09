using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedOrganizationContextTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var templateId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            migrationBuilder.Sql($@"
                INSERT INTO ""Templates"" (""Id"", ""Name"", ""Type"", ""Content"", ""Description"", ""CreatedAt"")
                SELECT '{templateId}', 'Organization Context', 2, '', 'Helpful information about your company or product', '{now:O}'
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Templates"" WHERE ""Type"" = 2
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Templates"" WHERE ""Type"" = 2;
            ");
        }
    }
}
