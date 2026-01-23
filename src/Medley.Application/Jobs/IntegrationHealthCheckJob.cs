using Hangfire.Console;
using Hangfire.Server;
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
    public async Task CheckAllIntegrationsHealthAsync(PerformContext context, CancellationToken cancellationToken)
    {
        await ExecuteWithTransactionAsync(async () =>
        {
            LogInfo(context, "Starting integration health checks");
            var integrations = _integrationService.Query().ToList();
            var checkedCount = 0;
            var errorCount = 0;

            foreach (var integration in integrations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    LogInfo(context, "Cancellation requested. Stopping health checks.");
                    break;
                }

                try
                {
                    await _integrationService.TestConnectionAsync(integration, cancellationToken);
                    checkedCount++;
                    LogDebug($"Health check completed for integration {integration.Id} ({integration.Name})");
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogError(context, ex, $"Health check failed for integration {integration.Id} ({integration.Name})");
                }
            }

            LogInfo(context, $"Periodic health check completed. Checked: {checkedCount}, Errors: {errorCount}");
        });
    }

    /// <summary>
    /// Performs health check on a specific integration
    /// </summary>
    /// <param name="integrationId">The ID of the integration to check</param>
    public async Task CheckIntegrationHealthAsync(PerformContext context, CancellationToken cancellationToken, Guid integrationId)
    {
        // If integrationId is Guid.Empty, check all integrations
        if (integrationId == Guid.Empty)
        {
            await CheckAllIntegrationsHealthAsync(context, cancellationToken);
            return;
        }

        await ExecuteWithTransactionAsync(async () =>
        {
            var integration = await _integrationService.GetByIdAsync(integrationId, cancellationToken);
            if (integration == null)
            {
                LogWarning(context, $"Integration {integrationId} not found for health check");
                return;
            }

            await _integrationService.TestConnectionAsync(integration, cancellationToken);
            
            LogInfo(context, $"Health check completed for integration {integration.Id} ({integration.Name})");
        });
    }
}
