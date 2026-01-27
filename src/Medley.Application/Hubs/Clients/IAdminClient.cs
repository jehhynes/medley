namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Strongly-typed interface for AdminHub server-to-client methods
/// </summary>
public interface IAdminClient
{
    /// <summary>
    /// Notifies admin clients of integration status updates
    /// </summary>
    Task IntegrationStatusUpdate(IntegrationStatusUpdatePayload payload);

    /// <summary>
    /// Notifies admin clients that fragment extraction has started
    /// </summary>
    Task FragmentExtractionStarted(FragmentExtractionStartedPayload payload);

    /// <summary>
    /// Notifies admin clients that fragment extraction has completed
    /// </summary>
    Task FragmentExtractionComplete(FragmentExtractionCompletePayload payload);
}
