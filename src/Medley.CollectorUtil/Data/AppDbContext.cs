using Microsoft.EntityFrameworkCore;

namespace Medley.CollectorUtil.Data;

public class AppDbContext : DbContext
{
    public DbSet<ApiKey> ApiKeys { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Medley.CollectorUtil",
            "apikeys.db"
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
    }
}
