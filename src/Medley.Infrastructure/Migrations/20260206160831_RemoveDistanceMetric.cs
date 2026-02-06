using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDistanceMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "distance_metric",
                table: "clustering_sessions");

            migrationBuilder.DropColumn(
                name: "distance_threshold",
                table: "clustering_sessions");

            migrationBuilder.DropColumn(
                name: "linkage",
                table: "clustering_sessions");

            migrationBuilder.DropColumn(
                name: "max_cluster_size",
                table: "clustering_sessions");

            migrationBuilder.DropColumn(
                name: "min_cluster_size",
                table: "clustering_sessions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "distance_threshold",
                table: "clustering_sessions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "linkage",
                table: "clustering_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_cluster_size",
                table: "clustering_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_cluster_size",
                table: "clustering_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "distance_metric",
                table: "clustering_sessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
