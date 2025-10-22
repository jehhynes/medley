using Medley.Domain.Enums;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send integration status update to all connected clients
    /// </summary>
    /// <param name="integrationId">The ID of the integration</param>
    /// <param name="status">The new connection status</param>
    /// <param name="message">Optional status message</param>
    Task SendIntegrationStatusUpdateAsync(Guid integrationId, ConnectionStatus status, string? message = null);
}
