using Microsoft.EntityFrameworkCore;

namespace Medley.CollectorUtil.Data;

public class ConfigurationService
{
    public async Task<string> GetWorkspaceAsync()
    {
        using var context = new AppDbContext();
        var config = await context.Configurations
            .FirstOrDefaultAsync(c => c.Key == "Workspace");
        
        return config?.Value ?? string.Empty;
    }
    
    public async Task SaveWorkspaceAsync(string workspace)
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
}
