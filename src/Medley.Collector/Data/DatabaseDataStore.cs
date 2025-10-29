using Google.Apis.Util.Store;

namespace Medley.Collector.Data;

/// <summary>
/// Custom IDataStore implementation that stores Google OAuth tokens in the database
/// </summary>
public class DatabaseDataStore : IDataStore
{
    private readonly ConfigurationService _configurationService;

    public DatabaseDataStore()
    {
        _configurationService = new ConfigurationService();
    }

    public async Task StoreAsync<T>(string key, T value)
    {
        var serialized = System.Text.Json.JsonSerializer.Serialize(value);
        await _configurationService.SaveGoogleTokenAsync(key, serialized);
    }

    public async Task DeleteAsync<T>(string key)
    {
        await _configurationService.DeleteGoogleTokenAsync(key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var serialized = await _configurationService.GetGoogleTokenAsync(key);
        
        if (string.IsNullOrEmpty(serialized))
        {
            return default;
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(serialized);
        }
        catch
        {
            return default;
        }
    }

    public async Task ClearAsync()
    {
        await _configurationService.DeleteAllGoogleTokensAsync();
    }
}
