using Medley.Infrastructure.Data;
using Medley.Tests.Integration.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Medley.Tests.Integration;

public class UnitOfWorkDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _dbContainer;
    private readonly bool _useTestContainers;
    private readonly string? _connectionString;
    private readonly List<NpgsqlDataSource> _dataSources = new();
    private readonly List<string> _createdDatabases = new();
    private string _baseConnectionString = null!;

    public UnitOfWorkDatabaseFixture()
    {
        var settings = TestConfigurationHelper.LoadSettings();
        _useTestContainers = settings.UseTestContainers;
        _connectionString = settings.ConnectionString;

        // Only create container for Docker mode
        if (_useTestContainers)
        {
            _dbContainer = new PostgreSqlBuilder()
                .WithImage("pgvector/pgvector:pg18")
                .WithDatabase("medley_test")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();
        }
    }

    public async Task InitializeAsync()
    {
        if (_useTestContainers)
        {
            // Use Testcontainers (Docker mode)
            await _dbContainer!.StartAsync();
            _baseConnectionString = _dbContainer.GetConnectionString();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "UseTestContainers is false but ConnectionString is not configured in appsettings.json");
            }

            // Use local PostgreSQL connection string directly
            _baseConnectionString = _connectionString;
        }
    }

    public async Task DisposeAsync()
    {
        // Dispose all created data sources
        foreach (var dataSource in _dataSources)
        {
            dataSource.Dispose();
        }
        _dataSources.Clear();

        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
        else if (!_useTestContainers && !string.IsNullOrWhiteSpace(_connectionString))
        {
            // Drop all created test databases when not using testcontainers
            foreach (var databaseName in _createdDatabases)
            {
                try
                {
                    await using var adminConnection = new NpgsqlConnection(_baseConnectionString);
                    await adminConnection.OpenAsync();
                    
                    // Terminate any active connections to the test database
                    await using var terminateCmd = new NpgsqlCommand(
                        $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{databaseName}' AND pid <> pg_backend_pid()",
                        adminConnection);
                    await terminateCmd.ExecuteNonQueryAsync();
                    
                    // Drop the test database
                    await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\"", adminConnection);
                    await dropCmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    // Swallow exceptions during cleanup to avoid masking test failures
                }
            }
            _createdDatabases.Clear();
        }
    }

    /// <summary>
    /// Creates a new database for each test to avoid transaction conflicts.
    /// Each database gets its own data source that will be disposed when the fixture is disposed.
    /// </summary>
    public async Task<ApplicationDbContext> CreateIsolatedDbContextAsync()
    {
        var databaseName = $"medley_test_{Guid.NewGuid():N}";
        
        // Build connection string for the new test database
        var builder = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = databaseName
        };
        var connectionString = builder.ConnectionString;

        // First, create the database using the admin connection
        await using (var adminConnection = new NpgsqlConnection(_baseConnectionString))
        {
            await adminConnection.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", adminConnection);
            await cmd.ExecuteNonQueryAsync();
        }

        // Track the created database for cleanup
        _createdDatabases.Add(databaseName);

        // Create a new data source for this isolated database
        var dataSource = ApplicationDbContextFactory.CreateDataSource(connectionString);
        _dataSources.Add(dataSource);

        var context = ApplicationDbContextFactory.CreateDbContext(dataSource);
        
        // Enable pgvector extension (required before EnsureCreatedAsync since migrations aren't run)
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        
        // Create the database schema
        await context.Database.EnsureCreatedAsync();
        
        return context;
    }
}