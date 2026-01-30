using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveObservationCluster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_observations_observation_clusters_observation_cluster_id",
                table: "observations");

            migrationBuilder.DropTable(
                name: "observation_clusters");

            migrationBuilder.DropIndex(
                name: "ix_observations_observation_cluster_id",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "observation_cluster_id",
                table: "observations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "observation_cluster_id",
                table: "observations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "observation_clusters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_observation_clusters", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_observations_observation_cluster_id",
                table: "observations",
                column: "observation_cluster_id");

            migrationBuilder.AddForeignKey(
                name: "fk_observations_observation_clusters_observation_cluster_id",
                table: "observations",
                column: "observation_cluster_id",
                principalTable: "observation_clusters",
                principalColumn: "id");
        }
    }
}
