using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using EFCore.NamingConventions;

namespace Medley.Infrastructure.Data;

/// <summary>
/// Factory for creating ApplicationDbContext instances with proper pgvector configuration.
/// Note: For production use, prefer registering NpgsqlDataSource as a singleton in DI
/// to avoid resource leaks. This factory is primarily for testing scenarios.
/// </summary>
public static class ApplicationDbContextFactory
{
    /// <summary>
    /// Creates a NpgsqlDataSource with pgvector support.
    /// IMPORTANT: Caller is responsible for disposing the returned data source.
    /// </summary>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <returns>Configured NpgsqlDataSource that must be disposed</returns>
    public static NpgsqlDataSource CreateDataSource(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        return dataSourceBuilder.Build();
    }

    /// <summary>
    /// Creates DbContextOptions with pgvector support using a provided data source.
    /// Recommended for scenarios where data source lifetime is managed externally.
    /// </summary>
    /// <param name="dataSource">NpgsqlDataSource with vector support</param>
    /// <param name="suppressWarnings">Whether to suppress pending model changes warnings</param>
    /// <returns>Configured DbContextOptions for ApplicationDbContext</returns>
    public static DbContextOptions<ApplicationDbContext> CreateOptions(
        NpgsqlDataSource dataSource,
        bool suppressWarnings = false)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dataSource, o => o.UseVector())
            .UseSnakeCaseNamingConvention();

        if (suppressWarnings)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        return optionsBuilder.Options;
    }

    /// <summary>
    /// Creates a new ApplicationDbContext instance with pgvector support using a data source.
    /// </summary>
    /// <param name="dataSource">NpgsqlDataSource with vector support</param>
    /// <returns>Configured ApplicationDbContext instance</returns>
    public static ApplicationDbContext CreateDbContext(NpgsqlDataSource dataSource)
    {
        var options = CreateOptions(dataSource);
        return new ApplicationDbContext(options);
    }

}
