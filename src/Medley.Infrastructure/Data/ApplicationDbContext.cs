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

    public DbSet<UserAuditLog> UserAuditLogs { get; set; }
    public DbSet<Fragment> Fragments { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Integration> Integrations { get; set; }
    public DbSet<Source> Sources { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<ArticleType> ArticleTypes { get; set; }
    public DbSet<ArticleVersion> ArticleVersions { get; set; }
    public DbSet<Insight> Insights { get; set; }
    public DbSet<Observation> Observations { get; set; }
    public DbSet<Finding> Findings { get; set; }
    public DbSet<ObservationCluster> ObservationClusters { get; set; }
    public DbSet<AiPrompt> AiPrompts { get; set; }
    public DbSet<TagType> TagTypes { get; set; }
    public DbSet<TagOption> TagOptions { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<ChatConversation> ChatConversations { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Plan> Plans { get; set; }
    public DbSet<PlanFragment> PlanFragments { get; set; }
    public DbSet<AiTokenUsage> AiTokenUsages { get; set; }
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public DbSet<Speaker> Speakers { get; set; }

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

        // Configure many-to-many relationship between Source and Speaker
        builder.Entity<Source>()
            .HasMany(s => s.Speakers)
            .WithMany(sp => sp.Sources)
            .UsingEntity(j => j.ToTable("source_speakers"));

        // Configure one-to-many relationship for PrimarySpeaker
        builder.Entity<Source>()
            .HasOne(s => s.PrimarySpeaker)
            .WithMany()
            .HasForeignKey(s => s.PrimarySpeakerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}