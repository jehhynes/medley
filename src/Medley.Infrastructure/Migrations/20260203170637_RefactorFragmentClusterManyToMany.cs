using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFragmentClusterManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clustering_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    method = table.Column<int>(type: "integer", nullable: false),
                    linkage = table.Column<int>(type: "integer", nullable: true),
                    distance_threshold = table.Column<double>(type: "double precision", nullable: true),
                    min_cluster_size = table.Column<int>(type: "integer", nullable: false),
                    max_cluster_size = table.Column<int>(type: "integer", nullable: true),
                    fragment_count = table.Column<int>(type: "integer", nullable: false),
                    cluster_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    status_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clustering_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clusters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clustering_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_number = table.Column<int>(type: "integer", nullable: false),
                    fragment_count = table.Column<int>(type: "integer", nullable: false),
                    centroid = table.Column<Vector>(type: "vector(2000)", nullable: true),
                    intra_cluster_distance = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clusters", x => x.id);
                    table.ForeignKey(
                        name: "fk_clusters_clustering_sessions_clustering_session_id",
                        column: x => x.clustering_session_id,
                        principalTable: "clustering_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cluster_fragment",
                columns: table => new
                {
                    clusters_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fragments_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cluster_fragment", x => new { x.clusters_id, x.fragments_id });
                    table.ForeignKey(
                        name: "fk_cluster_fragment_clusters_clusters_id",
                        column: x => x.clusters_id,
                        principalTable: "clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cluster_fragment_fragments_fragments_id",
                        column: x => x.fragments_id,
                        principalTable: "fragments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cluster_fragment_fragments_id",
                table: "cluster_fragment",
                column: "fragments_id");

            migrationBuilder.CreateIndex(
                name: "ix_clusters_clustering_session_id",
                table: "clusters",
                column: "clustering_session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cluster_fragment");

            migrationBuilder.DropTable(
                name: "clusters");

            migrationBuilder.DropTable(
                name: "clustering_sessions");
        }
    }
}
