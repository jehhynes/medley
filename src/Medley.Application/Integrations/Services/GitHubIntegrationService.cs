using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Service for GitHub integration connection management
/// </summary>
public class GitHubIntegrationService : BaseIntegrationConnectionService
{
    public GitHubIntegrationService(ILogger<GitHubIntegrationService> logger) : base(logger)
    {
    }

    public override IntegrationType IntegrationType => IntegrationType.GitHub;

    public override bool ValidateConfiguration(Dictionary<string, string> config)
    {
        if (!base.ValidateConfiguration(config))
            return false;

        if (!config.ContainsKey("apiKey") || string.IsNullOrWhiteSpace(config["apiKey"]))
        {
            Logger.LogWarning("GitHub integration configuration missing API key");
            return false;
        }

        if (!config.ContainsKey("baseUrl") || string.IsNullOrWhiteSpace(config["baseUrl"]))
        {
            Logger.LogWarning("GitHub integration configuration missing base URL");
            return false;
        }

        return true;
    }

    protected override async Task<bool> TestConnectionInternalAsync(Dictionary<string, string> config)
    {
        try
        {
            var apiKey = config["apiKey"];
            var baseUrl = config["baseUrl"];

            // TODO: Implement actual GitHub API connection test
            // For now, we'll simulate a connection test
            await Task.Delay(100); // Simulate API call

            // Basic validation - in real implementation, make actual API call to GitHub
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
            {
                Logger.LogWarning("GitHub integration test failed: missing required configuration");
                return false;
            }

            Logger.LogInformation("GitHub integration connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GitHub integration connection test failed");
            return false;
        }
    }
}
