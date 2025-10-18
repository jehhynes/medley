using Medley.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Medley.Tests.Integration;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private NpgsqlDataSource? _dataSource;
    public string ConnectionString { get; private set; } = null!;

    public DatabaseFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("medley_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        ConnectionString = _dbContainer.GetConnectionString();

        // Create data source once for the fixture lifetime
        _dataSource = ApplicationDbContextFactory.CreateDataSource(ConnectionString);

        // Create and migrate the database once
        using var context = ApplicationDbContextFactory.CreateDbContext(_dataSource);
        
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        _dataSource?.Dispose();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Gets the data source for creating connections with vector support
    /// </summary>
    public NpgsqlDataSource DataSource
    {
        get
        {
            if (_dataSource == null)
                throw new InvalidOperationException("DatabaseFixture not initialized. Ensure InitializeAsync was called.");
            return _dataSource;
        }
    }

    /// <summary>
    /// Creates a new DbContext instance for testing using the shared data source
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        return ApplicationDbContextFactory.CreateDbContext(DataSource);
    }
}