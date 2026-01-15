using Medley.Domain.Entities;
using Medley.Infrastructure.Data.Translators;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Medley.Infrastructure.Data;

/// <summary>
/// Application database context for Entity Framework Core
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// User audit log entries for authentication and authorization events
    /// </summary>
    public DbSet<UserAuditLog> UserAuditLogs { get; set; } = null!;

    /// <summary>
    /// Knowledge fragments with vector embeddings for semantic similarity
    /// </summary>
    public DbSet<Fragment> Fragments { get; set; } = null!;
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<Integration> Integrations { get; set; } = null!;
    public DbSet<Source> Sources { get; set; } = null!;
    public DbSet<Article> Articles { get; set; } = null!;
    public DbSet<ArticleType> ArticleTypes { get; set; } = null!;
    public DbSet<ArticleVersion> ArticleVersions { get; set; } = null!;
    public DbSet<Insight> Insights { get; set; } = null!;
    public DbSet<Observation> Observations { get; set; } = null!;
    public DbSet<Finding> Findings { get; set; } = null!;
    public DbSet<ObservationCluster> ObservationClusters { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;
    public DbSet<TagType> TagTypes { get; set; } = null!;
    public DbSet<TagOption> TagOptions { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    
    /// <summary>
    /// Chat conversations about articles
    /// </summary>
    public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
    
    /// <summary>
    /// Messages in chat conversations
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    
    /// <summary>
    /// Article improvement plans
    /// </summary>
    public DbSet<Plan> Plans { get; set; } = null!;
    
    /// <summary>
    /// Fragment recommendations within plans
    /// </summary>
    public DbSet<PlanFragment> PlanFragments { get; set; } = null!;
    
    /// <summary>
    /// AI token usage tracking for cost monitoring and analytics
    /// </summary>
    public DbSet<AiTokenUsage> AiTokenUsages { get; set; } = null!;
    
    /// <summary>
    /// Data Protection keys for persisting encryption keys across deployments
    /// </summary>
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.ReplaceService<IMethodCallTranslatorProvider, CustomNpgsqlMethodCallTranslatorProvider>();

        //optionsBuilder.UseNpgsql("connString", o => o.UseVector());
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //builder.HasPostgresExtension("vector");

        // Configure circular relationship between Article and ChatConversation
        builder.Entity<Article>()
            .HasOne(a => a.CurrentConversation)
            .WithOne()
            .HasForeignKey<Article>(a => a.CurrentConversationId)
            .OnDelete(DeleteBehavior.SetNull); // When conversation is deleted, set Article.CurrentConversationId to null (not cascade)

        builder.Entity<ChatConversation>()
            .HasOne(c => c.Article)
            .WithMany(a => a.ChatConversations)
            .HasForeignKey(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade); // When article is deleted, delete all conversations
    }
}