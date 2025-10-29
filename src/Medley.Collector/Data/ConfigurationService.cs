using Medley.Collector.Models;
using Microsoft.EntityFrameworkCore;

namespace Medley.Collector.Data;

public class ConfigurationService
{
    public async Task<string> GetFellowWorkspaceAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "Workspace");
        
        return config?.Value ?? string.Empty;
    }
    
    public async Task SaveFellowWorkspaceAsync(string workspace)
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "Workspace");
        
        if (config == null)
        {
            config = new Configuration { Key = "Workspace" };
            context.Configurations.Add(config);
        }
        
        config.Value = workspace?.Trim() ?? string.Empty;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public async Task<string> GetGoogleClientIdAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleClientId");
        
        return config?.Value ?? string.Empty;
    }

    public async Task SaveGoogleClientIdAsync(string clientId)
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleClientId");
        
        if (config == null)
        {
            config = new Configuration { Key = "GoogleClientId" };
            context.Configurations.Add(config);
        }
        
        config.Value = clientId?.Trim() ?? string.Empty;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public async Task<string> GetGoogleClientSecretAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleClientSecret");
        
        return config?.Value ?? string.Empty;
    }

    public async Task SaveGoogleClientSecretAsync(string clientSecret)
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleClientSecret");
        
        if (config == null)
        {
            config = new Configuration { Key = "GoogleClientSecret" };
            context.Configurations.Add(config);
        }
        
        config.Value = clientSecret?.Trim() ?? string.Empty;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public async Task<List<BrowserCookie>> GetGoogleBrowserCookiesAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleBrowserCookies");
        
        if (string.IsNullOrEmpty(config?.Value))
        {
            return new List<BrowserCookie>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<BrowserCookie>>(config.Value) 
                   ?? new List<BrowserCookie>();
        }
        catch
        {
            return new List<BrowserCookie>();
        }
    }

    public async Task SaveGoogleBrowserCookiesAsync(List<BrowserCookie> cookies)
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleBrowserCookies");
        
        if (config == null)
        {
            config = new Configuration { Key = "GoogleBrowserCookies" };
            context.Configurations.Add(config);
        }
        
        var cookiesJson = System.Text.Json.JsonSerializer.Serialize(cookies);
        config.Value = cookiesJson;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public async Task DeleteGoogleBrowserCookiesAsync()
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleBrowserCookies");
        
        if (config != null)
        {
            context.Configurations.Remove(config);
            await context.SaveChangesAsync();
        }
    }

    // Google OAuth Token methods
    public async Task<string> GetGoogleTokenAsync(string key)
    {
        using var context = new AppDbContext();
        var tokenKey = $"GoogleToken_{key}";
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == tokenKey);
        
        return config?.Value ?? string.Empty;
    }

    public async Task SaveGoogleTokenAsync(string key, string tokenJson)
    {
        using var context = new AppDbContext();
        var tokenKey = $"GoogleToken_{key}";
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == tokenKey);
        
        if (config == null)
        {
            config = new Configuration { Key = tokenKey };
            context.Configurations.Add(config);
        }
        
        config.Value = tokenJson ?? string.Empty;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }

    public async Task DeleteGoogleTokenAsync(string key)
    {
        using var context = new AppDbContext();
        var tokenKey = $"GoogleToken_{key}";
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == tokenKey);
        
        if (config != null)
        {
            context.Configurations.Remove(config);
            await context.SaveChangesAsync();
        }
    }

    public async Task DeleteAllGoogleTokensAsync()
    {
        using var context = new AppDbContext();
        
        var tokens = await context.Configurations
            .Where(c => c.Key.StartsWith("GoogleToken_"))
            .ToListAsync();
        
        if (tokens.Count > 0)
        {
            context.Configurations.RemoveRange(tokens);
            await context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasGoogleTokenAsync()
    {
        using var context = new AppDbContext();
        return await context.Configurations
            .AnyAsync(c => c.Key.StartsWith("GoogleToken_"));
    }

    public async Task<string> GetGoogleDriveFolderIdAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleDriveFolderId");
        
        return config?.Value ?? string.Empty;
    }

    public async Task SaveGoogleDriveFolderIdAsync(string folderId)
    {
        using var context = new AppDbContext();
        
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "GoogleDriveFolderId");
        
        if (config == null)
        {
            config = new Configuration { Key = "GoogleDriveFolderId" };
            context.Configurations.Add(config);
        }
        
        config.Value = folderId?.Trim() ?? string.Empty;
        config.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
    }
}