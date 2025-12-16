using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medley.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_ArticleTypes_ArticleTypeId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Articles_ParentArticleId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_FindingInsight_Findings_FindingsId",
                table: "FindingInsight");

            migrationBuilder.DropForeignKey(
                name: "FK_FindingInsight_Insights_InsightsId",
                table: "FindingInsight");

            migrationBuilder.DropForeignKey(
                name: "FK_FindingObservation_Findings_FindingsId",
                table: "FindingObservation");

            migrationBuilder.DropForeignKey(
                name: "FK_FindingObservation_Observations_ObservationsId",
                table: "FindingObservation");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Articles_ArticleId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Fragments_ClusteredIntoId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments");

            migrationBuilder.DropForeignKey(
                name: "FK_Observations_ObservationClusters_ObservationClusterId",
                table: "Observations");

            migrationBuilder.DropForeignKey(
                name: "FK_Observations_Sources_SourceId",
                table: "Observations");

            migrationBuilder.DropForeignKey(
                name: "FK_Sources_Integrations_IntegrationId",
                table: "Sources");

            migrationBuilder.DropForeignKey(
                name: "FK_TagOptions_TagTypes_TagTypeId",
                table: "TagOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Sources_SourceId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagOptions_TagOptionId",
                table: "Tags");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagTypes_TagTypeId",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Templates",
                table: "Templates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tags",
                table: "Tags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Sources",
                table: "Sources");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Observations",
                table: "Observations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Integrations",
                table: "Integrations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Insights",
                table: "Insights");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Fragments",
                table: "Fragments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Findings",
                table: "Findings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Articles",
                table: "Articles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagTypes",
                table: "TagTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TagOptions",
                table: "TagOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ObservationClusters",
                table: "ObservationClusters");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FindingObservation",
                table: "FindingObservation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FindingInsight",
                table: "FindingInsight");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ArticleTypes",
                table: "ArticleTypes");

            migrationBuilder.RenameTable(
                name: "Templates",
                newName: "templates");

            migrationBuilder.RenameTable(
                name: "Tags",
                newName: "tags");

            migrationBuilder.RenameTable(
                name: "Sources",
                newName: "sources");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "organizations");

            migrationBuilder.RenameTable(
                name: "Observations",
                newName: "observations");

            migrationBuilder.RenameTable(
                name: "Integrations",
                newName: "integrations");

            migrationBuilder.RenameTable(
                name: "Insights",
                newName: "insights");

            migrationBuilder.RenameTable(
                name: "Fragments",
                newName: "fragments");

            migrationBuilder.RenameTable(
                name: "Findings",
                newName: "findings");

            migrationBuilder.RenameTable(
                name: "Articles",
                newName: "articles");

            migrationBuilder.RenameTable(
                name: "UserAuditLogs",
                newName: "user_audit_logs");

            migrationBuilder.RenameTable(
                name: "TagTypes",
                newName: "tag_types");

            migrationBuilder.RenameTable(
                name: "TagOptions",
                newName: "tag_options");

            migrationBuilder.RenameTable(
                name: "ObservationClusters",
                newName: "observation_clusters");

            migrationBuilder.RenameTable(
                name: "FindingObservation",
                newName: "finding_observation");

            migrationBuilder.RenameTable(
                name: "FindingInsight",
                newName: "finding_insight");

            migrationBuilder.RenameTable(
                name: "ArticleTypes",
                newName: "article_types");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "templates",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "templates",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "templates",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "templates",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "templates",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "templates",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "templates",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "tags",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tags",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TagTypeId",
                table: "tags",
                newName: "tag_type_id");

            migrationBuilder.RenameColumn(
                name: "TagOptionId",
                table: "tags",
                newName: "tag_option_id");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "tags",
                newName: "source_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tags",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_TagTypeId",
                table: "tags",
                newName: "ix_tags_tag_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_TagOptionId",
                table: "tags",
                newName: "ix_tags_tag_option_id");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_SourceId_TagTypeId",
                table: "tags",
                newName: "ix_tags_source_id_tag_type_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "sources",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "sources",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "sources",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "sources",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sources",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TagsGenerated",
                table: "sources",
                newName: "tags_generated");

            migrationBuilder.RenameColumn(
                name: "MetadataType",
                table: "sources",
                newName: "metadata_type");

            migrationBuilder.RenameColumn(
                name: "MetadataJson",
                table: "sources",
                newName: "metadata_json");

            migrationBuilder.RenameColumn(
                name: "IsInternal",
                table: "sources",
                newName: "is_internal");

            migrationBuilder.RenameColumn(
                name: "IntegrationId",
                table: "sources",
                newName: "integration_id");

            migrationBuilder.RenameColumn(
                name: "ExtractionStatus",
                table: "sources",
                newName: "extraction_status");

            migrationBuilder.RenameColumn(
                name: "ExtractionMessage",
                table: "sources",
                newName: "extraction_message");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "sources",
                newName: "external_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sources",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_Sources_IntegrationId",
                table: "sources",
                newName: "ix_sources_integration_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "organizations",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "organizations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "EnableSmartTagging",
                table: "organizations",
                newName: "enable_smart_tagging");

            migrationBuilder.RenameColumn(
                name: "EmailDomain",
                table: "organizations",
                newName: "email_domain");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "observations",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Embedding",
                table: "observations",
                newName: "embedding");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "observations",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "observations",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "observations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "observations",
                newName: "source_id");

            migrationBuilder.RenameColumn(
                name: "SourceContext",
                table: "observations",
                newName: "source_context");

            migrationBuilder.RenameColumn(
                name: "ObservationClusterId",
                table: "observations",
                newName: "observation_cluster_id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "observations",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "observations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConfidenceScore",
                table: "observations",
                newName: "confidence_score");

            migrationBuilder.RenameIndex(
                name: "IX_Observations_SourceId",
                table: "observations",
                newName: "ix_observations_source_id");

            migrationBuilder.RenameIndex(
                name: "IX_Observations_ObservationClusterId",
                table: "observations",
                newName: "ix_observations_observation_cluster_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "integrations",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "integrations",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "integrations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "integrations",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "LastHealthCheckAt",
                table: "integrations",
                newName: "last_health_check_at");

            migrationBuilder.RenameColumn(
                name: "InitialSyncCompleted",
                table: "integrations",
                newName: "initial_sync_completed");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "integrations",
                newName: "display_name");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "integrations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BaseUrl",
                table: "integrations",
                newName: "base_url");

            migrationBuilder.RenameColumn(
                name: "ApiKey",
                table: "integrations",
                newName: "api_key");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "insights",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "insights",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "insights",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Priority",
                table: "insights",
                newName: "priority");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "insights",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "insights",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "insights",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "fragments",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "fragments",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "Embedding",
                table: "fragments",
                newName: "embedding");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "fragments",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Confidence",
                table: "fragments",
                newName: "confidence");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "fragments",
                newName: "category");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "fragments",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SourceId",
                table: "fragments",
                newName: "source_id");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "fragments",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "IsCluster",
                table: "fragments",
                newName: "is_cluster");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "fragments",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConfidenceComment",
                table: "fragments",
                newName: "confidence_comment");

            migrationBuilder.RenameColumn(
                name: "ClusteringProcessed",
                table: "fragments",
                newName: "clustering_processed");

            migrationBuilder.RenameColumn(
                name: "ClusteredIntoId",
                table: "fragments",
                newName: "clustered_into_id");

            migrationBuilder.RenameColumn(
                name: "ArticleId",
                table: "fragments",
                newName: "article_id");

            migrationBuilder.RenameIndex(
                name: "IX_Fragments_SourceId",
                table: "fragments",
                newName: "ix_fragments_source_id");

            migrationBuilder.RenameIndex(
                name: "IX_Fragments_ClusteredIntoId",
                table: "fragments",
                newName: "ix_fragments_clustered_into_id");

            migrationBuilder.RenameIndex(
                name: "IX_Fragments_ArticleId",
                table: "fragments",
                newName: "ix_fragments_article_id");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "findings",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "findings",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "findings",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "findings",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConfidenceScore",
                table: "findings",
                newName: "confidence_score");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "AspNetUserTokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetUserTokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserTokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserTokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AspNetUsers",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUsers",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "AspNetUsers",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "AspNetUsers",
                newName: "two_factor_enabled");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "AspNetUsers",
                newName: "security_stamp");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "AspNetUsers",
                newName: "phone_number_confirmed");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "AspNetUsers",
                newName: "phone_number");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "AspNetUsers",
                newName: "password_hash");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "AspNetUsers",
                newName: "normalized_user_name");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "AspNetUsers",
                newName: "normalized_email");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "AspNetUsers",
                newName: "lockout_end");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "AspNetUsers",
                newName: "lockout_enabled");

            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "AspNetUsers",
                newName: "last_modified_at");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "AspNetUsers",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "AspNetUsers",
                newName: "full_name");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "AspNetUsers",
                newName: "email_confirmed");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AspNetUsers",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetUsers",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "AspNetUsers",
                newName: "access_failed_count");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetUserRoles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserRoles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                newName: "ix_asp_net_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserLogins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "AspNetUserLogins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "AspNetUserLogins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "AspNetUserLogins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                newName: "ix_asp_net_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetUserClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "AspNetUserClaims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetUserClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetUserClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                newName: "ix_asp_net_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AspNetRoles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "AspNetRoles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "AspNetRoles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AspNetRoleClaims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "AspNetRoleClaims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "AspNetRoleClaims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "AspNetRoleClaims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                newName: "ix_asp_net_role_claims_role_id");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "articles",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Summary",
                table: "articles",
                newName: "summary");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "articles",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                table: "articles",
                newName: "metadata");

            migrationBuilder.RenameColumn(
                name: "Content",
                table: "articles",
                newName: "content");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "articles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "PublishedAt",
                table: "articles",
                newName: "published_at");

            migrationBuilder.RenameColumn(
                name: "ParentArticleId",
                table: "articles",
                newName: "parent_article_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "articles",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ArticleTypeId",
                table: "articles",
                newName: "article_type_id");

            migrationBuilder.RenameIndex(
                name: "IX_Articles_ParentArticleId",
                table: "articles",
                newName: "ix_articles_parent_article_id");

            migrationBuilder.RenameIndex(
                name: "IX_Articles_ArticleTypeId",
                table: "articles",
                newName: "ix_articles_article_type_id");

            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "user_audit_logs",
                newName: "timestamp");

            migrationBuilder.RenameColumn(
                name: "Success",
                table: "user_audit_logs",
                newName: "success");

            migrationBuilder.RenameColumn(
                name: "Details",
                table: "user_audit_logs",
                newName: "details");

            migrationBuilder.RenameColumn(
                name: "Action",
                table: "user_audit_logs",
                newName: "action");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_audit_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "user_audit_logs",
                newName: "user_name");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_audit_logs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "user_audit_logs",
                newName: "ip_address");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLogs_UserId",
                table: "user_audit_logs",
                newName: "ix_user_audit_logs_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLogs_Timestamp",
                table: "user_audit_logs",
                newName: "ix_user_audit_logs_timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_UserAuditLogs_Action",
                table: "user_audit_logs",
                newName: "ix_user_audit_logs_action");

            migrationBuilder.RenameColumn(
                name: "Prompt",
                table: "tag_types",
                newName: "prompt");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "tag_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tag_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsConstrained",
                table: "tag_types",
                newName: "is_constrained");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tag_types",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_TagTypes_Name",
                table: "tag_types",
                newName: "ix_tag_types_name");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "tag_options",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tag_options",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TagTypeId",
                table: "tag_options",
                newName: "tag_type_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tag_options",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_TagOptions_TagTypeId_Value",
                table: "tag_options",
                newName: "ix_tag_options_tag_type_id_value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "observation_clusters",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "observation_clusters",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "observation_clusters",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "observation_clusters",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ObservationsId",
                table: "finding_observation",
                newName: "observations_id");

            migrationBuilder.RenameColumn(
                name: "FindingsId",
                table: "finding_observation",
                newName: "findings_id");

            migrationBuilder.RenameIndex(
                name: "IX_FindingObservation_ObservationsId",
                table: "finding_observation",
                newName: "ix_finding_observation_observations_id");

            migrationBuilder.RenameColumn(
                name: "InsightsId",
                table: "finding_insight",
                newName: "insights_id");

            migrationBuilder.RenameColumn(
                name: "FindingsId",
                table: "finding_insight",
                newName: "findings_id");

            migrationBuilder.RenameIndex(
                name: "IX_FindingInsight_InsightsId",
                table: "finding_insight",
                newName: "ix_finding_insight_insights_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "article_types",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "article_types",
                newName: "icon");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "article_types",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "article_types",
                newName: "created_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_templates",
                table: "templates",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tags",
                table: "tags",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_sources",
                table: "sources",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_organizations",
                table: "organizations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_observations",
                table: "observations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_integrations",
                table: "integrations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_insights",
                table: "insights",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_fragments",
                table: "fragments",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_findings",
                table: "findings",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens",
                columns: new[] { "user_id", "login_provider", "name" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles",
                columns: new[] { "user_id", "role_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins",
                columns: new[] { "login_provider", "provider_key" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_articles",
                table: "articles",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_audit_logs",
                table: "user_audit_logs",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tag_types",
                table: "tag_types",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_tag_options",
                table: "tag_options",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_observation_clusters",
                table: "observation_clusters",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_finding_observation",
                table: "finding_observation",
                columns: new[] { "findings_id", "observations_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_finding_insight",
                table: "finding_insight",
                columns: new[] { "findings_id", "insights_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_article_types",
                table: "article_types",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_articles_article_types_article_type_id",
                table: "articles",
                column: "article_type_id",
                principalTable: "article_types",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_articles_articles_parent_article_id",
                table: "articles",
                column: "parent_article_id",
                principalTable: "articles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id",
                principalTable: "AspNetRoles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens",
                column: "user_id",
                principalTable: "AspNetUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_finding_insight_findings_findings_id",
                table: "finding_insight",
                column: "findings_id",
                principalTable: "findings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_finding_insight_insights_insights_id",
                table: "finding_insight",
                column: "insights_id",
                principalTable: "insights",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_finding_observation_findings_findings_id",
                table: "finding_observation",
                column: "findings_id",
                principalTable: "findings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_finding_observation_observations_observations_id",
                table: "finding_observation",
                column: "observations_id",
                principalTable: "observations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_articles_article_id",
                table: "fragments",
                column: "article_id",
                principalTable: "articles",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_fragments_clustered_into_id",
                table: "fragments",
                column: "clustered_into_id",
                principalTable: "fragments",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_fragments_sources_source_id",
                table: "fragments",
                column: "source_id",
                principalTable: "sources",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_observations_observation_clusters_observation_cluster_id",
                table: "observations",
                column: "observation_cluster_id",
                principalTable: "observation_clusters",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_observations_sources_source_id",
                table: "observations",
                column: "source_id",
                principalTable: "sources",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_sources_integrations_integration_id",
                table: "sources",
                column: "integration_id",
                principalTable: "integrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tag_options_tag_types_tag_type_id",
                table: "tag_options",
                column: "tag_type_id",
                principalTable: "tag_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tags_sources_source_id",
                table: "tags",
                column: "source_id",
                principalTable: "sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_tags_tag_options_tag_option_id",
                table: "tags",
                column: "tag_option_id",
                principalTable: "tag_options",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_tags_tag_types_tag_type_id",
                table: "tags",
                column: "tag_type_id",
                principalTable: "tag_types",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_articles_article_types_article_type_id",
                table: "articles");

            migrationBuilder.DropForeignKey(
                name: "fk_articles_articles_parent_article_id",
                table: "articles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_claims_asp_net_users_user_id",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_logins_asp_net_users_user_id",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_roles_asp_net_users_user_id",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "fk_finding_insight_findings_findings_id",
                table: "finding_insight");

            migrationBuilder.DropForeignKey(
                name: "fk_finding_insight_insights_insights_id",
                table: "finding_insight");

            migrationBuilder.DropForeignKey(
                name: "fk_finding_observation_findings_findings_id",
                table: "finding_observation");

            migrationBuilder.DropForeignKey(
                name: "fk_finding_observation_observations_observations_id",
                table: "finding_observation");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_articles_article_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_fragments_clustered_into_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_fragments_sources_source_id",
                table: "fragments");

            migrationBuilder.DropForeignKey(
                name: "fk_observations_observation_clusters_observation_cluster_id",
                table: "observations");

            migrationBuilder.DropForeignKey(
                name: "fk_observations_sources_source_id",
                table: "observations");

            migrationBuilder.DropForeignKey(
                name: "fk_sources_integrations_integration_id",
                table: "sources");

            migrationBuilder.DropForeignKey(
                name: "fk_tag_options_tag_types_tag_type_id",
                table: "tag_options");

            migrationBuilder.DropForeignKey(
                name: "fk_tags_sources_source_id",
                table: "tags");

            migrationBuilder.DropForeignKey(
                name: "fk_tags_tag_options_tag_option_id",
                table: "tags");

            migrationBuilder.DropForeignKey(
                name: "fk_tags_tag_types_tag_type_id",
                table: "tags");

            migrationBuilder.DropPrimaryKey(
                name: "pk_templates",
                table: "templates");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tags",
                table: "tags");

            migrationBuilder.DropPrimaryKey(
                name: "pk_sources",
                table: "sources");

            migrationBuilder.DropPrimaryKey(
                name: "pk_organizations",
                table: "organizations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_observations",
                table: "observations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_integrations",
                table: "integrations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_insights",
                table: "insights");

            migrationBuilder.DropPrimaryKey(
                name: "pk_fragments",
                table: "fragments");

            migrationBuilder.DropPrimaryKey(
                name: "pk_findings",
                table: "findings");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_tokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_users",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_roles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_logins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_claims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_roles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_role_claims",
                table: "AspNetRoleClaims");

            migrationBuilder.DropPrimaryKey(
                name: "pk_articles",
                table: "articles");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_audit_logs",
                table: "user_audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tag_types",
                table: "tag_types");

            migrationBuilder.DropPrimaryKey(
                name: "pk_tag_options",
                table: "tag_options");

            migrationBuilder.DropPrimaryKey(
                name: "pk_observation_clusters",
                table: "observation_clusters");

            migrationBuilder.DropPrimaryKey(
                name: "pk_finding_observation",
                table: "finding_observation");

            migrationBuilder.DropPrimaryKey(
                name: "pk_finding_insight",
                table: "finding_insight");

            migrationBuilder.DropPrimaryKey(
                name: "pk_article_types",
                table: "article_types");

            migrationBuilder.RenameTable(
                name: "templates",
                newName: "Templates");

            migrationBuilder.RenameTable(
                name: "tags",
                newName: "Tags");

            migrationBuilder.RenameTable(
                name: "sources",
                newName: "Sources");

            migrationBuilder.RenameTable(
                name: "organizations",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "observations",
                newName: "Observations");

            migrationBuilder.RenameTable(
                name: "integrations",
                newName: "Integrations");

            migrationBuilder.RenameTable(
                name: "insights",
                newName: "Insights");

            migrationBuilder.RenameTable(
                name: "fragments",
                newName: "Fragments");

            migrationBuilder.RenameTable(
                name: "findings",
                newName: "Findings");

            migrationBuilder.RenameTable(
                name: "articles",
                newName: "Articles");

            migrationBuilder.RenameTable(
                name: "user_audit_logs",
                newName: "UserAuditLogs");

            migrationBuilder.RenameTable(
                name: "tag_types",
                newName: "TagTypes");

            migrationBuilder.RenameTable(
                name: "tag_options",
                newName: "TagOptions");

            migrationBuilder.RenameTable(
                name: "observation_clusters",
                newName: "ObservationClusters");

            migrationBuilder.RenameTable(
                name: "finding_observation",
                newName: "FindingObservation");

            migrationBuilder.RenameTable(
                name: "finding_insight",
                newName: "FindingInsight");

            migrationBuilder.RenameTable(
                name: "article_types",
                newName: "ArticleTypes");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Templates",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Templates",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Templates",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Templates",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Templates",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "Templates",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Templates",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "Tags",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Tags",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tag_type_id",
                table: "Tags",
                newName: "TagTypeId");

            migrationBuilder.RenameColumn(
                name: "tag_option_id",
                table: "Tags",
                newName: "TagOptionId");

            migrationBuilder.RenameColumn(
                name: "source_id",
                table: "Tags",
                newName: "SourceId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Tags",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_tags_tag_type_id",
                table: "Tags",
                newName: "IX_Tags_TagTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_tags_tag_option_id",
                table: "Tags",
                newName: "IX_Tags_TagOptionId");

            migrationBuilder.RenameIndex(
                name: "ix_tags_source_id_tag_type_id",
                table: "Tags",
                newName: "IX_Tags_SourceId_TagTypeId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Sources",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Sources",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "Sources",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Sources",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Sources",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tags_generated",
                table: "Sources",
                newName: "TagsGenerated");

            migrationBuilder.RenameColumn(
                name: "metadata_type",
                table: "Sources",
                newName: "MetadataType");

            migrationBuilder.RenameColumn(
                name: "metadata_json",
                table: "Sources",
                newName: "MetadataJson");

            migrationBuilder.RenameColumn(
                name: "is_internal",
                table: "Sources",
                newName: "IsInternal");

            migrationBuilder.RenameColumn(
                name: "integration_id",
                table: "Sources",
                newName: "IntegrationId");

            migrationBuilder.RenameColumn(
                name: "extraction_status",
                table: "Sources",
                newName: "ExtractionStatus");

            migrationBuilder.RenameColumn(
                name: "extraction_message",
                table: "Sources",
                newName: "ExtractionMessage");

            migrationBuilder.RenameColumn(
                name: "external_id",
                table: "Sources",
                newName: "ExternalId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Sources",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_sources_integration_id",
                table: "Sources",
                newName: "IX_Sources_IntegrationId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Organizations",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Organizations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "enable_smart_tagging",
                table: "Organizations",
                newName: "EnableSmartTagging");

            migrationBuilder.RenameColumn(
                name: "email_domain",
                table: "Organizations",
                newName: "EmailDomain");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Observations",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "embedding",
                table: "Observations",
                newName: "Embedding");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Observations",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "Observations",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Observations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "source_id",
                table: "Observations",
                newName: "SourceId");

            migrationBuilder.RenameColumn(
                name: "source_context",
                table: "Observations",
                newName: "SourceContext");

            migrationBuilder.RenameColumn(
                name: "observation_cluster_id",
                table: "Observations",
                newName: "ObservationClusterId");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "Observations",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Observations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "confidence_score",
                table: "Observations",
                newName: "ConfidenceScore");

            migrationBuilder.RenameIndex(
                name: "ix_observations_source_id",
                table: "Observations",
                newName: "IX_Observations_SourceId");

            migrationBuilder.RenameIndex(
                name: "ix_observations_observation_cluster_id",
                table: "Observations",
                newName: "IX_Observations_ObservationClusterId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Integrations",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Integrations",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Integrations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "Integrations",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "last_health_check_at",
                table: "Integrations",
                newName: "LastHealthCheckAt");

            migrationBuilder.RenameColumn(
                name: "initial_sync_completed",
                table: "Integrations",
                newName: "InitialSyncCompleted");

            migrationBuilder.RenameColumn(
                name: "display_name",
                table: "Integrations",
                newName: "DisplayName");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Integrations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "base_url",
                table: "Integrations",
                newName: "BaseUrl");

            migrationBuilder.RenameColumn(
                name: "api_key",
                table: "Integrations",
                newName: "ApiKey");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Insights",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Insights",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Insights",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "priority",
                table: "Insights",
                newName: "Priority");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Insights",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Insights",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Insights",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Fragments",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "Fragments",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "embedding",
                table: "Fragments",
                newName: "Embedding");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Fragments",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "confidence",
                table: "Fragments",
                newName: "Confidence");

            migrationBuilder.RenameColumn(
                name: "category",
                table: "Fragments",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Fragments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "source_id",
                table: "Fragments",
                newName: "SourceId");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "Fragments",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "is_cluster",
                table: "Fragments",
                newName: "IsCluster");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Fragments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "confidence_comment",
                table: "Fragments",
                newName: "ConfidenceComment");

            migrationBuilder.RenameColumn(
                name: "clustering_processed",
                table: "Fragments",
                newName: "ClusteringProcessed");

            migrationBuilder.RenameColumn(
                name: "clustered_into_id",
                table: "Fragments",
                newName: "ClusteredIntoId");

            migrationBuilder.RenameColumn(
                name: "article_id",
                table: "Fragments",
                newName: "ArticleId");

            migrationBuilder.RenameIndex(
                name: "ix_fragments_source_id",
                table: "Fragments",
                newName: "IX_Fragments_SourceId");

            migrationBuilder.RenameIndex(
                name: "ix_fragments_clustered_into_id",
                table: "Fragments",
                newName: "IX_Fragments_ClusteredIntoId");

            migrationBuilder.RenameIndex(
                name: "ix_fragments_article_id",
                table: "Fragments",
                newName: "IX_Fragments_ArticleId");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "Findings",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Findings",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Findings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Findings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "confidence_score",
                table: "Findings",
                newName: "ConfidenceScore");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "AspNetUserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetUserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "AspNetUsers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUsers",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "AspNetUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "two_factor_enabled",
                table: "AspNetUsers",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "security_stamp",
                table: "AspNetUsers",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "phone_number_confirmed",
                table: "AspNetUsers",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "phone_number",
                table: "AspNetUsers",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "password_hash",
                table: "AspNetUsers",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "normalized_user_name",
                table: "AspNetUsers",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "normalized_email",
                table: "AspNetUsers",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "lockout_end",
                table: "AspNetUsers",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "lockout_enabled",
                table: "AspNetUsers",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "last_modified_at",
                table: "AspNetUsers",
                newName: "LastModifiedAt");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "AspNetUsers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "full_name",
                table: "AspNetUsers",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "email_confirmed",
                table: "AspNetUsers",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "AspNetUsers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetUsers",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "access_failed_count",
                table: "AspNetUsers",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetUserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserRoles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "provider_display_name",
                table: "AspNetUserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "provider_key",
                table: "AspNetUserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "login_provider",
                table: "AspNetUserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetUserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AspNetUserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetUserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetUserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "AspNetRoles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "normalized_name",
                table: "AspNetRoles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "concurrency_stamp",
                table: "AspNetRoles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AspNetRoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "role_id",
                table: "AspNetRoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "claim_value",
                table: "AspNetRoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "claim_type",
                table: "AspNetRoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Articles",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "summary",
                table: "Articles",
                newName: "Summary");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "Articles",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "metadata",
                table: "Articles",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "content",
                table: "Articles",
                newName: "Content");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Articles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "published_at",
                table: "Articles",
                newName: "PublishedAt");

            migrationBuilder.RenameColumn(
                name: "parent_article_id",
                table: "Articles",
                newName: "ParentArticleId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Articles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "article_type_id",
                table: "Articles",
                newName: "ArticleTypeId");

            migrationBuilder.RenameIndex(
                name: "ix_articles_parent_article_id",
                table: "Articles",
                newName: "IX_Articles_ParentArticleId");

            migrationBuilder.RenameIndex(
                name: "ix_articles_article_type_id",
                table: "Articles",
                newName: "IX_Articles_ArticleTypeId");

            migrationBuilder.RenameColumn(
                name: "timestamp",
                table: "UserAuditLogs",
                newName: "Timestamp");

            migrationBuilder.RenameColumn(
                name: "success",
                table: "UserAuditLogs",
                newName: "Success");

            migrationBuilder.RenameColumn(
                name: "details",
                table: "UserAuditLogs",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "action",
                table: "UserAuditLogs",
                newName: "Action");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserAuditLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_name",
                table: "UserAuditLogs",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "UserAuditLogs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "UserAuditLogs",
                newName: "IpAddress");

            migrationBuilder.RenameIndex(
                name: "ix_user_audit_logs_user_id",
                table: "UserAuditLogs",
                newName: "IX_UserAuditLogs_UserId");

            migrationBuilder.RenameIndex(
                name: "ix_user_audit_logs_timestamp",
                table: "UserAuditLogs",
                newName: "IX_UserAuditLogs_Timestamp");

            migrationBuilder.RenameIndex(
                name: "ix_user_audit_logs_action",
                table: "UserAuditLogs",
                newName: "IX_UserAuditLogs_Action");

            migrationBuilder.RenameColumn(
                name: "prompt",
                table: "TagTypes",
                newName: "Prompt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "TagTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TagTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_constrained",
                table: "TagTypes",
                newName: "IsConstrained");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "TagTypes",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_tag_types_name",
                table: "TagTypes",
                newName: "IX_TagTypes_Name");

            migrationBuilder.RenameColumn(
                name: "value",
                table: "TagOptions",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TagOptions",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tag_type_id",
                table: "TagOptions",
                newName: "TagTypeId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "TagOptions",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "ix_tag_options_tag_type_id_value",
                table: "TagOptions",
                newName: "IX_TagOptions_TagTypeId_Value");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ObservationClusters",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "ObservationClusters",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ObservationClusters",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ObservationClusters",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "observations_id",
                table: "FindingObservation",
                newName: "ObservationsId");

            migrationBuilder.RenameColumn(
                name: "findings_id",
                table: "FindingObservation",
                newName: "FindingsId");

            migrationBuilder.RenameIndex(
                name: "ix_finding_observation_observations_id",
                table: "FindingObservation",
                newName: "IX_FindingObservation_ObservationsId");

            migrationBuilder.RenameColumn(
                name: "insights_id",
                table: "FindingInsight",
                newName: "InsightsId");

            migrationBuilder.RenameColumn(
                name: "findings_id",
                table: "FindingInsight",
                newName: "FindingsId");

            migrationBuilder.RenameIndex(
                name: "ix_finding_insight_insights_id",
                table: "FindingInsight",
                newName: "IX_FindingInsight_InsightsId");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ArticleTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "icon",
                table: "ArticleTypes",
                newName: "Icon");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ArticleTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ArticleTypes",
                newName: "CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Templates",
                table: "Templates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tags",
                table: "Tags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Sources",
                table: "Sources",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Organizations",
                table: "Organizations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Observations",
                table: "Observations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Integrations",
                table: "Integrations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Insights",
                table: "Insights",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fragments",
                table: "Fragments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Findings",
                table: "Findings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Articles",
                table: "Articles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagTypes",
                table: "TagTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TagOptions",
                table: "TagOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ObservationClusters",
                table: "ObservationClusters",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FindingObservation",
                table: "FindingObservation",
                columns: new[] { "FindingsId", "ObservationsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_FindingInsight",
                table: "FindingInsight",
                columns: new[] { "FindingsId", "InsightsId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ArticleTypes",
                table: "ArticleTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_ArticleTypes_ArticleTypeId",
                table: "Articles",
                column: "ArticleTypeId",
                principalTable: "ArticleTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Articles_ParentArticleId",
                table: "Articles",
                column: "ParentArticleId",
                principalTable: "Articles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FindingInsight_Findings_FindingsId",
                table: "FindingInsight",
                column: "FindingsId",
                principalTable: "Findings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FindingInsight_Insights_InsightsId",
                table: "FindingInsight",
                column: "InsightsId",
                principalTable: "Insights",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FindingObservation_Findings_FindingsId",
                table: "FindingObservation",
                column: "FindingsId",
                principalTable: "Findings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FindingObservation_Observations_ObservationsId",
                table: "FindingObservation",
                column: "ObservationsId",
                principalTable: "Observations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Articles_ArticleId",
                table: "Fragments",
                column: "ArticleId",
                principalTable: "Articles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Fragments_ClusteredIntoId",
                table: "Fragments",
                column: "ClusteredIntoId",
                principalTable: "Fragments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Fragments_Sources_SourceId",
                table: "Fragments",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Observations_ObservationClusters_ObservationClusterId",
                table: "Observations",
                column: "ObservationClusterId",
                principalTable: "ObservationClusters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Observations_Sources_SourceId",
                table: "Observations",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Sources_Integrations_IntegrationId",
                table: "Sources",
                column: "IntegrationId",
                principalTable: "Integrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TagOptions_TagTypes_TagTypeId",
                table: "TagOptions",
                column: "TagTypeId",
                principalTable: "TagTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Sources_SourceId",
                table: "Tags",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagOptions_TagOptionId",
                table: "Tags",
                column: "TagOptionId",
                principalTable: "TagOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagTypes_TagTypeId",
                table: "Tags",
                column: "TagTypeId",
                principalTable: "TagTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
