namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Payload for ArticleCreated event
/// </summary>
public record ArticleCreatedPayload
{
    public required string ArticleId { get; init; }
    public required string Title { get; init; }
    public string? ParentArticleId { get; init; }
    public string? ArticleTypeId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleUpdated event
/// </summary>
public record ArticleUpdatedPayload
{
    public required string ArticleId { get; init; }
    public required string Title { get; init; }
    public string? ArticleTypeId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleDeleted event
/// </summary>
public record ArticleDeletedPayload
{
    public required string ArticleId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleMoved event
/// </summary>
public record ArticleMovedPayload
{
    public required string ArticleId { get; init; }
    public string? OldParentId { get; init; }
    public string? NewParentId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleAssignmentChanged event
/// </summary>
public record ArticleAssignmentChangedPayload
{
    public required string ArticleId { get; init; }
    public string? UserId { get; init; }
    public string? UserName { get; init; }
    public string? UserInitials { get; init; }
    public string? UserColor { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for VersionCreated event
/// </summary>
public record VersionCreatedPayload
{
    public required string ArticleId { get; init; }
    public required string VersionId { get; init; }
    public required string VersionNumber { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Payload for PlanGenerated event
/// </summary>
public record PlanGeneratedPayload
{
    public required string ArticleId { get; init; }
    public required string PlanId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleVersionCreated event
/// </summary>
public record ArticleVersionCreatedPayload
{
    public required string ArticleId { get; init; }
    public required string VersionId { get; init; }
    public required string VersionNumber { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatTurnStarted event
/// </summary>
public record ChatTurnStartedPayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
}

/// <summary>
/// Payload for ChatTurnComplete event
/// </summary>
public record ChatTurnCompletePayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
}

/// <summary>
/// Payload for ChatMessageStreaming event
/// </summary>
public record ChatMessageStreamingPayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
    public string? Text { get; init; }
    public string? ToolName { get; init; }
    public string? ToolCallId { get; init; }
    public string? ToolDisplay { get; init; }
    public string[]? ToolResultIds { get; init; }
    public bool? IsError { get; init; }
    public string? MessageId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatToolInvoked event
/// </summary>
public record ChatToolInvokedPayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
    public required string ToolName { get; init; }
    public required string ToolCallId { get; init; }
    public string? ToolDisplay { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatToolCompleted event
/// </summary>
public record ChatToolCompletedPayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
    public required string ToolCallId { get; init; }
    public string[]? ToolResultIds { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatMessageComplete event
/// </summary>
public record ChatMessageCompletePayload
{
    public required string Id { get; init; }
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatError event
/// </summary>
public record ChatErrorPayload
{
    public required string ConversationId { get; init; }
    public required string ArticleId { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ReceiveMessage event
/// </summary>
public record ReceiveMessagePayload
{
    public required string ArticleId { get; init; }
    public required string UserName { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
