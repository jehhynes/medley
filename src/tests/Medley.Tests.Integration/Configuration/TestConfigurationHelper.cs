using Microsoft.Extensions.Configuration;

namespace Medley.Tests.Integration.Configuration;

/// <summary>
/// Helper class for loading test configuration settings.
/// </summary>
public static class TestConfigurationHelper
{
    private static TestDatabaseSettings? _cachedSettings;
    private static readonly object _lock = new object();

    /// <summary>
    /// Loads test database settings from appsettings.Test.json.
    /// Returns default settings (Docker mode) if the file doesn't exist or cannot be loaded.
    /// </summary>
    public static TestDatabaseSettings LoadSettings()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        lock (_lock)
        {
            if (_cachedSettings != null)
                return _cachedSettings;

            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                    .Build();

                var settings = new TestDatabaseSettings();
                configuration.GetSection("TestDatabase").Bind(settings);
                
                _cachedSettings = settings;
                return settings;
            }
            catch
            {
                // If configuration fails, return default settings (Docker mode)
                _cachedSettings = new TestDatabaseSettings();
                return _cachedSettings;
            }
        }
    }
}

