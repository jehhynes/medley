using Medley.Infrastructure.Data;
using Medley.Tests.Integration.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Medley.Tests.Integration;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _dbContainer;
    private readonly bool _useTestContainers;
    private readonly string? _localConnectionString;
    private NpgsqlDataSource? _dataSource;
    private string? _testDatabaseName;
    public string? ConnectionString { get; private set; }

    public DatabaseFixture()
    {
        var settings = TestConfigurationHelper.LoadSettings();
        _useTestContainers = settings.UseTestContainers;
        _localConnectionString = settings.LocalConnectionString;

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
            ConnectionString = _dbContainer.GetConnectionString();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(_localConnectionString))
            {
                throw new InvalidOperationException(
                    "UseTestContainers is false but LocalConnectionString is not configured in appsettings.Test.json");
            }

            // Use local PostgreSQL connection string directly
            // Create a unique test database name to avoid conflicts
            _testDatabaseName = $"medley_test_{Guid.NewGuid():N}";
            await using (var adminConnection = new NpgsqlConnection(_localConnectionString))
            {
                await adminConnection.OpenAsync();
                
                // Create new test database
                await using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{_testDatabaseName}\"", adminConnection);
                await createCmd.ExecuteNonQueryAsync();
            }

            // Build connection string for the test database
            var builder = new NpgsqlConnectionStringBuilder(_localConnectionString)
            {
                Database = _testDatabaseName
            };
            ConnectionString = builder.ConnectionString;
        }

        // Create data source once for the fixture lifetime
        _dataSource = ApplicationDbContextFactory.CreateDataSource(ConnectionString);

        // Create and migrate the database once (suppress pending model changes warning for tests)
        var options = ApplicationDbContextFactory.CreateOptions(_dataSource, suppressWarnings: true);
        using var context = new ApplicationDbContext(options);
        
        // Enable pgvector extension before running migrations
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;");
        
        await context.Database.MigrateAsync();
        
        // Reload types after migrations to pick up the vector extension
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await connection.ReloadTypesAsync();
        await connection.CloseAsync();
    }

    public async Task DisposeAsync()
    {
        _dataSource?.Dispose();
        
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
        else if (!string.IsNullOrWhiteSpace(_testDatabaseName) && !string.IsNullOrWhiteSpace(_localConnectionString))
        {
            // Drop the test database when not using testcontainers
            try
            {
                await using var adminConnection = new NpgsqlConnection(_localConnectionString);
                await adminConnection.OpenAsync();
                
                // Terminate any active connections to the test database
                await using var terminateCmd = new NpgsqlCommand(
                    $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_testDatabaseName}' AND pid <> pg_backend_pid()",
                    adminConnection);
                await terminateCmd.ExecuteNonQueryAsync();
                
                // Drop the test database
                await using var dropCmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"", adminConnection);
                await dropCmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Swallow exceptions during cleanup to avoid masking test failures
            }
        }
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