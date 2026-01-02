using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiTokenUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_token_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: true),
                    output_tokens = table.Column<int>(type: "integer", nullable: true),
                    embedding_tokens = table.Column<int>(type: "integer", nullable: true),
                    model_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    operation_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    call_stack = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    related_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_token_usages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_token_usages_model_name",
                table: "ai_token_usages",
                column: "model_name");

            migrationBuilder.CreateIndex(
                name: "ix_ai_token_usages_related_entity_type_related_entity_id",
                table: "ai_token_usages",
                columns: new[] { "related_entity_type", "related_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_token_usages_service_name",
                table: "ai_token_usages",
                column: "service_name");

            migrationBuilder.CreateIndex(
                name: "ix_ai_token_usages_timestamp",
                table: "ai_token_usages",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_token_usages");
        }
    }
}
