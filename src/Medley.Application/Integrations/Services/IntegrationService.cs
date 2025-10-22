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

    public async Task<ConnectionStatus> TestConnectionAsync(Integration integration)
    {
        if (!_connectionServices.TryGetValue(integration.Type, out var connectionService))
        {
            _logger.LogWarning("No connection service found for integration type {IntegrationType}", integration.Type);
            integration.Status = ConnectionStatus.Error;
            integration.LastHealthCheckAt = DateTimeOffset.UtcNow;
            await _integrationRepository.SaveAsync(integration);
            return ConnectionStatus.Error;
        }

        var status = await connectionService.TestConnectionAsync(integration);
        integration.Status = status;
        integration.LastHealthCheckAt = DateTimeOffset.UtcNow;
        
        await _integrationRepository.SaveAsync(integration);
        
        _logger.LogInformation("Connection test completed for integration {IntegrationId}: {Status}", 
            integration.Id, status);
            
        return status;
    }
}
