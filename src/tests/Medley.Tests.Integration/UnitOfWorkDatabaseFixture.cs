using Medley.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Medley.Tests.Integration;

public class UnitOfWorkDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private readonly List<NpgsqlDataSource> _dataSources = new();
    private string _baseConnectionString = null!;
    private int _databaseCounter = 0;

    public UnitOfWorkDatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
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
        // Dispose all created data sources
        foreach (var dataSource in _dataSources)
        {
            dataSource.Dispose();
        }
        _dataSources.Clear();

        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates a new database for each test to avoid transaction conflicts.
    /// Each database gets its own data source that will be disposed when the fixture is disposed.
    /// </summary>
    public async Task<ApplicationDbContext> CreateIsolatedDbContextAsync()
    {
        var databaseName = $"medley_test_{Interlocked.Increment(ref _databaseCounter)}_{Guid.NewGuid():N}";
        var connectionString = _baseConnectionString.Replace("Database=postgres", $"Database={databaseName}");

        // Create a new data source for this isolated database
        var dataSource = ApplicationDbContextFactory.CreateDataSource(connectionString);
        _dataSources.Add(dataSource);

        var context = ApplicationDbContextFactory.CreateDbContext(dataSource);
        
        // Create the database with schema (this applies migrations automatically)
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }
}