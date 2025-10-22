using Medley.Application.Integrations.Interfaces;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job service for periodic integration health checks
/// </summary>
public class IntegrationHealthCheckJob : BaseHangfireJob<IntegrationHealthCheckJob>
{
    private readonly IIntegrationService _integrationService;

    public IntegrationHealthCheckJob(
        IIntegrationService integrationService,
        IUnitOfWork unitOfWork,
        ILogger<IntegrationHealthCheckJob> logger) : base(unitOfWork, logger)
    {
        _integrationService = integrationService;
    }

    /// <summary>
    /// Performs health checks on all integrations
    /// </summary>
    public async Task CheckAllIntegrationsHealthAsync()
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            var integrations = _integrationService.Query().ToList();
            var checkedCount = 0;
            var errorCount = 0;

            foreach (var integration in integrations)
            {
                try
                {
                    await _integrationService.TestConnectionAsync(integration);
                    checkedCount++;
                    _logger.LogDebug("Health check completed for integration {IntegrationId} ({IntegrationName})", 
                        integration.Id, integration.DisplayName);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Health check failed for integration {IntegrationId} ({IntegrationName})", 
                        integration.Id, integration.DisplayName);
                }
            }

            _logger.LogInformation("Periodic health check completed. Checked: {CheckedCount}, Errors: {ErrorCount}", 
                checkedCount, errorCount);
        });
    }

    /// <summary>
    /// Performs health check on a specific integration
    /// </summary>
    /// <param name="integrationId">The ID of the integration to check</param>
    public async Task CheckIntegrationHealthAsync(Guid integrationId)
    {
        // If integrationId is Guid.Empty, check all integrations
        if (integrationId == Guid.Empty)
        {
            await CheckAllIntegrationsHealthAsync();
            return;
        }

        await ExecuteWithTransactionAsync(async () =>
        {
            var integration = await _integrationService.GetByIdAsync(integrationId);
            if (integration == null)
            {
                _logger.LogWarning("Integration {IntegrationId} not found for health check", integrationId);
                return;
            }

            await _integrationService.TestConnectionAsync(integration);
            
            _logger.LogInformation("Health check completed for integration {IntegrationId} ({IntegrationName})", 
                integration.Id, integration.DisplayName);
        });
    }
}
