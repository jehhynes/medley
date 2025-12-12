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
    //private readonly INotificationService _notificationService;

    public IntegrationService(
        IRepository<Integration> integrationRepository,
        ILogger<IntegrationService> logger,
        IEnumerable<IIntegrationConnectionService> connectionServices
        //INotificationService notificationService
        )
    {
        _integrationRepository = integrationRepository;
        _logger = logger;
        _connectionServices = connectionServices.ToDictionary(s => s.IntegrationType, s => s);
        //_notificationService = notificationService;
    }

    public async Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _integrationRepository.GetByIdAsync(id, cancellationToken);
    }

    public IQueryable<Integration> Query()
    {
        return _integrationRepository.Query();
    }

    public async Task SaveAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        await _integrationRepository.SaveAsync(integration);
    }

    public async Task DeleteAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        // TODO: Implement delete functionality when IRepository supports it
        // For now, we'll throw a not implemented exception
        throw new NotImplementedException("Delete functionality not yet implemented in IRepository");
    }

    public async Task<ConnectionStatus> TestConnectionAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        if (!_connectionServices.TryGetValue(integration.Type, out var connectionService))
        {
            _logger.LogWarning("No connection service found for integration type {IntegrationType}", integration.Type);
            integration.Status = ConnectionStatus.Error;
            integration.LastHealthCheckAt = DateTimeOffset.UtcNow;
            await _integrationRepository.SaveAsync(integration);
            
            // Broadcast status update
            //await _notificationService.SendIntegrationStatusUpdateAsync(
            //    integration.Id, 
            //    ConnectionStatus.Error, 
            //    "No connection service available for this integration type");
            
            return ConnectionStatus.Error;
        }

        var previousStatus = integration.Status;
        var status = await connectionService.TestConnectionAsync(integration, cancellationToken);
        integration.Status = status;
        integration.LastHealthCheckAt = DateTimeOffset.UtcNow;
        
        await _integrationRepository.SaveAsync(integration);
        
        // Broadcast status update if status changed
        //if (previousStatus != status)
        //{
        //    var message = status switch
        //    {
        //        ConnectionStatus.Connected => "Connection test successful",
        //        ConnectionStatus.Disconnected => "Connection test failed - service unavailable",
        //        ConnectionStatus.Error => "Connection test failed - configuration error",
        //        _ => "Connection status updated"
        //    };
            
        //    await _notificationService.SendIntegrationStatusUpdateAsync(integration.Id, status, message);
        //}
        
        _logger.LogInformation("Connection test completed for integration {IntegrationId}: {Status}", 
            integration.Id, status);
            
        return status;
    }
}
