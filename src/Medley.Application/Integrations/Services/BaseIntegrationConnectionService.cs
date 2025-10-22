using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Base class for integration connection services
/// </summary>
public abstract class BaseIntegrationConnectionService : IIntegrationConnectionService
{
    protected readonly ILogger Logger;

    protected BaseIntegrationConnectionService(ILogger logger)
    {
        Logger = logger;
    }

    public abstract IntegrationType IntegrationType { get; }

    public virtual bool ValidateConfiguration(Dictionary<string, string> config)
    {
        if (config == null || !config.Any())
        {
            Logger.LogWarning("Configuration is null or empty for {IntegrationType}", IntegrationType);
            return false;
        }

        return true;
    }

    public virtual async Task<bool> TestConnectionAsync(Integration integration)
    {
        try
        {
            if (integration.Type != IntegrationType)
            {
                Logger.LogWarning("Integration type {IntegrationType} does not match service type {ServiceType}", 
                    integration.Type, IntegrationType);
                return false;
            }

            var config = ParseConfiguration(integration.ConfigurationJson);
            if (config == null || !ValidateConfiguration(config))
            {
                Logger.LogWarning("Invalid configuration for {IntegrationType} integration {IntegrationId}", 
                    IntegrationType, integration.Id);
                return false;
            }

            return await TestConnectionInternalAsync(config);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error testing connection for {IntegrationType} integration {IntegrationId}", 
                IntegrationType, integration.Id);
            return false;
        }
    }

    public virtual async Task<ConnectionStatus> GetConnectionStatusAsync(Integration integration)
    {
        try
        {
            if (integration.Type != IntegrationType)
            {
                Logger.LogWarning("Integration type {IntegrationType} does not match service type {ServiceType}", 
                    integration.Type, IntegrationType);
                return ConnectionStatus.Disconnected;
            }

            var config = ParseConfiguration(integration.ConfigurationJson);
            if (config == null || !ValidateConfiguration(config))
            {
                return ConnectionStatus.Disconnected;
            }

            var isConnected = await TestConnectionInternalAsync(config);
            return isConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting connection status for {IntegrationType} integration {IntegrationId}", 
                IntegrationType, integration.Id);
            return ConnectionStatus.Error;
        }
    }

    protected abstract Task<bool> TestConnectionInternalAsync(Dictionary<string, string> config);

    protected Dictionary<string, string>? ParseConfiguration(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(configurationJson);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Failed to parse configuration JSON for {IntegrationType}", IntegrationType);
            return null;
        }
    }
}
