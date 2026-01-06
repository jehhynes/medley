using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "changes_summary",
                table: "plans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_plan_id",
                table: "plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "plans",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_plans_parent_plan_id",
                table: "plans",
                column: "parent_plan_id");

            migrationBuilder.AddForeignKey(
                name: "fk_plans_plans_parent_plan_id",
                table: "plans",
                column: "parent_plan_id",
                principalTable: "plans",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_plans_plans_parent_plan_id",
                table: "plans");

            migrationBuilder.DropIndex(
                name: "ix_plans_parent_plan_id",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "changes_summary",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "parent_plan_id",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "version",
                table: "plans");
        }
    }
}
