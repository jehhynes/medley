using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationPlanImplementation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "implementing_plan_id",
                table: "chat_conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_chat_conversations_implementing_plan_id",
                table: "chat_conversations",
                column: "implementing_plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_chat_conversations_plans_implementing_plan_id",
                table: "chat_conversations",
                column: "implementing_plan_id",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_chat_conversations_plans_implementing_plan_id",
                table: "chat_conversations");

            migrationBuilder.DropIndex(
                name: "ix_chat_conversations_implementing_plan_id",
                table: "chat_conversations");

            migrationBuilder.DropColumn(
                name: "implementing_plan_id",
                table: "chat_conversations");
        }
    }
}
