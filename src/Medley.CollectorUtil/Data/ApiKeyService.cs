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

        var existingKeys = await context.ApiKeys.ToListAsync();
        var incomingIds = apiKeys.Where(k => k.Id > 0).Select(k => k.Id).ToHashSet();

        // Delete keys that are no longer in the list
        var keysToDelete = existingKeys.Where(k => !incomingIds.Contains(k.Id)).ToList();
        context.ApiKeys.RemoveRange(keysToDelete);

        // Update or add keys
        foreach (var apiKey in apiKeys.Where(k => !string.IsNullOrWhiteSpace(k.Name) && !string.IsNullOrWhiteSpace(k.Key)))
        {
            if (apiKey.Id > 0)
            {
                // Update existing key
                var existing = existingKeys.FirstOrDefault(k => k.Id == apiKey.Id);
                if (existing != null)
                {
                    existing.Name = apiKey.Name.Trim();
                    existing.Key = apiKey.Key.Trim();
                    existing.IsEnabled = apiKey.IsEnabled;
                    context.ApiKeys.Update(existing);
                }
            }
            else
            {
                // Add new key
                context.ApiKeys.Add(new ApiKey
                {
                    Name = apiKey.Name.Trim(),
                    Key = apiKey.Key.Trim(),
                    IsEnabled = apiKey.IsEnabled,
                    CreatedAt = DateTime.UtcNow
                });
            }
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

    public async Task SetApiKeyEnabledAsync(int apiKeyId, bool isEnabled)
    {
        using var context = new AppDbContext();
        var apiKey = await context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey != null)
        {
            apiKey.IsEnabled = isEnabled;
            await context.SaveChangesAsync();
        }
    }
}
