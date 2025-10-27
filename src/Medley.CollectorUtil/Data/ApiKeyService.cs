using Microsoft.EntityFrameworkCore;

namespace Medley.CollectorUtil.Data;

public class ApiKeyService
{
    public async Task<List<ApiKey>> GetAllApiKeysAsync()
    {
        using var context = new AppDbContext();
        return await context.ApiKeys
            .OrderBy(k => k.CreatedAt)
            .ToListAsync();
    }
    
    public async Task SaveApiKeysAsync(List<ApiKey> apiKeys)
    {
        using var context = new AppDbContext();
        
        // Clear existing keys
        context.ApiKeys.RemoveRange(context.ApiKeys);
        
        // Add new keys
        foreach (var apiKey in apiKeys.Where(k => !string.IsNullOrWhiteSpace(k.Name) && !string.IsNullOrWhiteSpace(k.Key)))
        {
            context.ApiKeys.Add(new ApiKey 
            { 
                Name = apiKey.Name.Trim(),
                Key = apiKey.Key.Trim(),
                IsEnabled = apiKey.IsEnabled,
                CreatedAt = DateTime.UtcNow
            });
        }
        
        await context.SaveChangesAsync();
    }
    
    public async Task<List<ApiKey>> GetEnabledApiKeysAsync()
    {
        using var context = new AppDbContext();
        return await context.ApiKeys
            .Where(k => k.IsEnabled)
            .OrderBy(k => k.CreatedAt)
            .ToListAsync();
    }
}
