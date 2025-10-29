using Microsoft.EntityFrameworkCore;

namespace Medley.Collector.Data;

public class AppDbContext : DbContext
{
    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<MeetingTranscript> MeetingTranscripts { get; set; }
    public DbSet<Configuration> Configurations { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Medley.Collector",
            "user_data.db"
        );
        
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>()
            .HasIndex(a => a.Name)
            .IsUnique();
        
        modelBuilder.Entity<MeetingTranscript>()
            .HasIndex(m => m.ExternalId);
        
        modelBuilder.Entity<Configuration>()
            .HasIndex(c => c.Key)
            .IsUnique();
        
        // Configure many-to-many relationship
        modelBuilder.Entity<MeetingTranscript>()
            .HasMany(m => m.ApiKeys)
            .WithMany(a => a.MeetingTranscripts)
            .UsingEntity(j => j.ToTable("MeetingTranscriptApiKeys"));
    }
}
