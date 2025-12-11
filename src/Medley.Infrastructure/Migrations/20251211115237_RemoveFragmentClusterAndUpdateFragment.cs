using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFragmentClusterAndUpdateFragment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_FragmentClusters_FragmentClusterId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments");

            migrationBuilder.DropTable(
                name: "FragmentClusters");

            migrationBuilder.RenameColumn(
                name: "FragmentClusterId",
                table: "Fragments",
                newName: "ClusteredIntoId");

            migrationBuilder.RenameIndex(
                name: "IX_Fragments_FragmentClusterId",
                table: "Fragments",
                newName: "IX_Fragments_ClusteredIntoId");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceId",
                table: "Fragments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClusteringProcessed",
                table: "Fragments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCluster",
                table: "Fragments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Fragments_ClusteredIntoId",
                table: "Fragments",
                column: "ClusteredIntoId",
                principalTable: "Fragments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Fragments_ClusteredIntoId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "ClusteringProcessed",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "IsCluster",
                table: "Fragments");

            migrationBuilder.RenameColumn(
                name: "ClusteredIntoId",
                table: "Fragments",
                newName: "FragmentClusterId");

            migrationBuilder.RenameIndex(
                name: "IX_Fragments_ClusteredIntoId",
                table: "Fragments",
                newName: "IX_Fragments_FragmentClusterId");

            migrationBuilder.AlterColumn<Guid>(
                name: "SourceId",
                table: "Fragments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "FragmentClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FragmentClusters", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_FragmentClusters_FragmentClusterId",
                table: "Fragments",
                column: "FragmentClusterId",
                principalTable: "FragmentClusters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
