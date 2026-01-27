using Medley.Application.Interfaces;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;

namespace Medley.Infrastructure.Services;

/// <summary>
/// SignalR-based notification service for real-time updates
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<AdminHub, IAdminClient> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<AdminHub, IAdminClient> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Send integration status update to all connected clients
    /// </summary>
    public async Task SendIntegrationStatusUpdateAsync(Guid integrationId, ConnectionStatus status, string? message = null)
    {
        try
        {
            await _hubContext.Clients.Group("AdminNotifications")
                .IntegrationStatusUpdate(new IntegrationStatusUpdatePayload
                {
                    IntegrationId = integrationId,
                    Status = status.ToString(),
                    Message = message ?? string.Empty
                });
            
            _logger.LogDebug("Sent integration status update for {IntegrationId}: {Status}", 
                integrationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send integration status update for {IntegrationId}", integrationId);
        }
    }

    /// <summary>
    /// Send fragment extraction started notification
    /// </summary>
    public async Task SendFragmentExtractionStartedAsync(Guid sourceId)
    {
        try
        {
            await _hubContext.Clients.All
                .FragmentExtractionStarted(new FragmentExtractionStartedPayload
                {
                    SourceId = sourceId
                });
            
            _logger.LogDebug("Sent fragment extraction started notification for source {SourceId}", sourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send fragment extraction started notification for source {SourceId}", sourceId);
        }
    }

    /// <summary>
    /// Send fragment extraction completion notification
    /// </summary>
    public async Task SendFragmentExtractionCompleteAsync(Guid sourceId, int fragmentCount, bool success, string? message = null)
    {
        try
        {
            await _hubContext.Clients.All
                .FragmentExtractionComplete(new FragmentExtractionCompletePayload
                {
                    SourceId = sourceId,
                    FragmentCount = fragmentCount,
                    Success = success,
                    Message = message ?? string.Empty
                });
            
            _logger.LogDebug("Sent fragment extraction complete notification for source {SourceId}: {FragmentCount} fragments, Success: {Success}", 
                sourceId, fragmentCount, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send fragment extraction complete notification for source {SourceId}", sourceId);
        }
    }
}
