namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Payload for IntegrationStatusUpdate event
/// </summary>
public record IntegrationStatusUpdatePayload(
    Guid IntegrationId,
    string Status,
    string Message
);

/// <summary>
/// Payload for FragmentExtractionComplete event
/// </summary>
public record FragmentExtractionCompletePayload(
    Guid SourceId,
    int FragmentCount,
    bool Success,
    string Message
);
