using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CoreModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Fragments");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "Fragments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ArticleId",
                table: "Fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FragmentClusterId",
                table: "Fragments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FragmentClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FragmentClusters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Insights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObservationClusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObservationClusters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FindingInsight",
                columns: table => new
                {
                    FindingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsightsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingInsight", x => new { x.FindingsId, x.InsightsId });
                    table.ForeignKey(
                        name: "FK_FindingInsight_Findings_FindingsId",
                        column: x => x.FindingsId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FindingInsight_Insights_InsightsId",
                        column: x => x.InsightsId,
                        principalTable: "Insights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SourceContext = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ObservationClusterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_ObservationClusters_ObservationClusterId",
                        column: x => x.ObservationClusterId,
                        principalTable: "ObservationClusters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Observations_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FindingObservation",
                columns: table => new
                {
                    FindingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingObservation", x => new { x.FindingsId, x.ObservationsId });
                    table.ForeignKey(
                        name: "FK_FindingObservation_Findings_FindingsId",
                        column: x => x.FindingsId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FindingObservation_Observations_ObservationsId",
                        column: x => x.ObservationsId,
                        principalTable: "Observations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fragments_ArticleId",
                table: "Fragments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Fragments_FragmentClusterId",
                table: "Fragments",
                column: "FragmentClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Fragments_SourceId",
                table: "Fragments",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingInsight_InsightsId",
                table: "FindingInsight",
                column: "InsightsId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingObservation_ObservationsId",
                table: "FindingObservation",
                column: "ObservationsId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_ObservationClusterId",
                table: "Observations",
                column: "ObservationClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_SourceId",
                table: "Observations",
                column: "SourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Articles_ArticleId",
                table: "Fragments",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Articles_ArticleId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_FragmentClusters_FragmentClusterId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "FindingInsight");

            migrationBuilder.DropTable(
                name: "FindingObservation");

            migrationBuilder.DropTable(
                name: "FragmentClusters");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropTable(
                name: "Insights");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropTable(
                name: "ObservationClusters");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropIndex(
                name: "IX_Fragments_ArticleId",
                table: "Fragments");

            migrationBuilder.DropIndex(
                name: "IX_Fragments_FragmentClusterId",
                table: "Fragments");

            migrationBuilder.DropIndex(
                name: "IX_Fragments_SourceId",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "ArticleId",
                table: "Fragments");

            migrationBuilder.DropColumn(
                name: "FragmentClusterId",
                table: "Fragments");

            migrationBuilder.AlterColumn<string>(
                name: "SourceId",
                table: "Fragments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Fragments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
