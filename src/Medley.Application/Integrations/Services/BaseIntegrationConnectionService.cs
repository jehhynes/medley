using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;
using Microsoft.Extensions.Logging;

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

    public virtual async Task<ConnectionStatus> TestConnectionAsync(Integration integration)
    {
        try
        {
            if (integration.Type != IntegrationType)
            {
                Logger.LogWarning("Integration type {IntegrationType} does not match service type {ServiceType}", 
                    integration.Type, IntegrationType);
                return ConnectionStatus.Error;
            }

            if (string.IsNullOrWhiteSpace(integration.ApiKey) || string.IsNullOrWhiteSpace(integration.BaseUrl))
            {
                Logger.LogWarning("Missing ApiKey or BaseUrl for {IntegrationType} integration {IntegrationId}", 
                    IntegrationType, integration.Id);
                return ConnectionStatus.Disconnected;
            }

            var isConnected = await TestConnectionInternalAsync(integration.ApiKey, integration.BaseUrl);
            return isConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error testing connection for {IntegrationType} integration {IntegrationId}", 
                IntegrationType, integration.Id);
            return ConnectionStatus.Error;
        }
    }

    protected abstract Task<bool> TestConnectionInternalAsync(string apiKey, string baseUrl);
}
