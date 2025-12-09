using Medley.Domain.Entities;
using Medley.Infrastructure.Data.Translators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Medley.Infrastructure.Data;

/// <summary>
/// Application database context for Entity Framework Core
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
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
    public DbSet<Insight> Insights { get; set; } = null!;
    public DbSet<Observation> Observations { get; set; } = null!;
    public DbSet<Finding> Findings { get; set; } = null!;
    public DbSet<FragmentCluster> FragmentClusters { get; set; } = null!;
    public DbSet<ObservationCluster> ObservationClusters { get; set; } = null!;
    public DbSet<Template> Templates { get; set; } = null!;

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

        // Additional entity configurations can be added here if needed
        // Most configuration is handled via data annotations on entities
    }
}