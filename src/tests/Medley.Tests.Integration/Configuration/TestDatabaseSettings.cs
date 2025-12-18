namespace Medley.Tests.Integration.Configuration;

/// <summary>
/// Configuration settings for test database connection mode.
/// </summary>
public class TestDatabaseSettings
{
    /// <summary>
    /// Gets or sets whether to use Testcontainers (Docker) for test databases.
    /// Default is true (Docker mode).
    /// When false, LocalConnectionString must be provided for local PostgreSQL.
    /// </summary>
    public bool UseTestContainers { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection string for local PostgreSQL mode.
    /// Only used when UseTestContainers is false.
    /// Should point to the admin database (e.g., "postgres") for test database creation.
    /// </summary>
    public string? LocalConnectionString { get; set; }
}

