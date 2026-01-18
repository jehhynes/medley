namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Payload for IntegrationStatusUpdate event
/// </summary>
public record IntegrationStatusUpdatePayload
{
    public required Guid IntegrationId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Payload for FragmentExtractionComplete event
/// </summary>
public record FragmentExtractionCompletePayload
{
    public required Guid SourceId { get; init; }
    public required int FragmentCount { get; init; }
    public required bool Success { get; init; }
    public required string Message { get; init; }
}
