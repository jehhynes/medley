using Medley.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Medley.Tests.Infrastructure;

public class UnitOfWorkDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private string _baseConnectionString = null!;
    private int _databaseCounter = 0;

    public UnitOfWorkDatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("postgres") // Use default postgres database
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        _baseConnectionString = _dbContainer.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a new database for each test to avoid transaction conflicts
    /// </summary>
    public async Task<ApplicationDbContext> CreateIsolatedDbContextAsync()
    {
        var databaseName = $"medley_test_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid():N}";
        var connectionString = _baseConnectionString.Replace("Database=postgres", $"Database={databaseName}");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, o => o.UseVector())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        var context = new ApplicationDbContext(options);
        
        // Create the database with schema (this applies migrations automatically)
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }
}