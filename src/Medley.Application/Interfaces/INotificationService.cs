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

    /// <summary>
    /// Send fragment extraction started notification
    /// </summary>
    /// <param name="sourceId">The ID of the source</param>
    Task SendFragmentExtractionStartedAsync(Guid sourceId);

    /// <summary>
    /// Send fragment extraction completion notification
    /// </summary>
    /// <param name="sourceId">The ID of the source</param>
    /// <param name="fragmentCount">Number of fragments extracted</param>
    /// <param name="success">Whether the extraction was successful</param>
    /// <param name="message">Optional message</param>
    Task SendFragmentExtractionCompleteAsync(Guid sourceId, int fragmentCount, bool success, string? message = null);
}
