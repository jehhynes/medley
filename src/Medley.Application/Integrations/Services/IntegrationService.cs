using Medley.Application.Interfaces;
using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Service for managing integrations
/// </summary>
public class IntegrationService : IIntegrationService
{
    private readonly IRepository<Integration> _integrationRepository;
    private readonly ILogger<IntegrationService> _logger;
    private readonly Dictionary<IntegrationType, IIntegrationConnectionService> _connectionServices;

    public IntegrationService(
        IRepository<Integration> integrationRepository,
        ILogger<IntegrationService> logger,
        IEnumerable<IIntegrationConnectionService> connectionServices)
    {
        _integrationRepository = integrationRepository;
        _logger = logger;
        _connectionServices = connectionServices.ToDictionary(s => s.IntegrationType, s => s);
    }

    public async Task<Integration?> GetByIdAsync(Guid id)
    {
        return await _integrationRepository.GetByIdAsync(id);
    }

    public IQueryable<Integration> Query()
    {
        return _integrationRepository.Query();
    }

    public async Task SaveAsync(Integration integration)
    {
        await _integrationRepository.SaveAsync(integration);
    }

    public async Task DeleteAsync(Integration integration)
    {
        // TODO: Implement delete functionality when IRepository supports it
        // For now, we'll throw a not implemented exception
        throw new NotImplementedException("Delete functionality not yet implemented in IRepository");
    }

    public async Task<bool> TestConnectionAsync(Integration integration)
    {
        if (!_connectionServices.TryGetValue(integration.Type, out var connectionService))
        {
            _logger.LogWarning("No connection service found for integration type {IntegrationType}", integration.Type);
            return false;
        }

        return await connectionService.TestConnectionAsync(integration);
    }

    public async Task<ConnectionStatus> GetConnectionStatusAsync(Integration integration)
    {
        if (!_connectionServices.TryGetValue(integration.Type, out var connectionService))
        {
            _logger.LogWarning("No connection service found for integration type {IntegrationType}", integration.Type);
            return ConnectionStatus.Error;
        }

        return await connectionService.GetConnectionStatusAsync(integration);
    }

    public async Task UpdateHealthStatusAsync(Integration integration)
    {
        try
        {
            var status = await GetConnectionStatusAsync(integration);
            _logger.LogInformation("Health status updated for integration {IntegrationId}: {Status}", 
                integration.Id, status);
            
            // TODO: Update integration health status in database if needed
            // For now, we'll just log the status
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health status for integration {IntegrationId}", integration.Id);
        }
    }
}
