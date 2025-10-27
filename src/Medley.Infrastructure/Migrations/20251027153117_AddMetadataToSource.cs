using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataToSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "IntegrationId",
                table: "Sources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Sources",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_IntegrationId",
                table: "Sources",
                column: "IntegrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sources_Integrations_IntegrationId",
                table: "Sources",
                column: "IntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sources_Integrations_IntegrationId",
                table: "Sources");

            migrationBuilder.DropIndex(
                name: "IX_Sources_IntegrationId",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Sources");

            migrationBuilder.AlterColumn<Guid>(
                name: "IntegrationId",
                table: "Sources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
