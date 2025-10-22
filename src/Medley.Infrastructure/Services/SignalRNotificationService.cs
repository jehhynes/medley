using Medley.Application.Interfaces;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Medley.Application.Hubs;

namespace Medley.Infrastructure.Services;

/// <summary>
/// SignalR-based notification service for real-time updates
/// </summary>
public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<IntegrationStatusHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<IntegrationStatusHub> hubContext,
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
            await _hubContext.Clients.Group("IntegrationStatus")
                .SendAsync("IntegrationStatusUpdate", integrationId, status.ToString(), message ?? string.Empty);
            
            _logger.LogDebug("Sent integration status update for {IntegrationId}: {Status}", 
                integrationId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send integration status update for {IntegrationId}", integrationId);
        }
    }
}
