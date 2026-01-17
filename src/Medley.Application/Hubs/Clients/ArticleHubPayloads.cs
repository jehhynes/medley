namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Payload for ArticleCreated event
/// </summary>
public record ArticleCreatedPayload(
    string ArticleId,
    string Title,
    string? ParentArticleId,
    string? ArticleTypeId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleUpdated event
/// </summary>
public record ArticleUpdatedPayload(
    string ArticleId,
    string Title,
    string? ArticleTypeId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleDeleted event
/// </summary>
public record ArticleDeletedPayload(
    string ArticleId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleMoved event
/// </summary>
public record ArticleMovedPayload(
    string ArticleId,
    string? OldParentId,
    string? NewParentId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleAssignmentChanged event
/// </summary>
public record ArticleAssignmentChangedPayload(
    string ArticleId,
    string? UserId,
    string? UserName,
    string? UserInitials,
    string? UserColor,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for VersionCreated event
/// </summary>
public record VersionCreatedPayload(
    string ArticleId,
    string VersionId,
    string VersionNumber,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Payload for PlanGenerated event
/// </summary>
public record PlanGeneratedPayload(
    string ArticleId,
    string PlanId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ArticleVersionCreated event
/// </summary>
public record ArticleVersionCreatedPayload(
    string ArticleId,
    string VersionId,
    string VersionNumber,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ChatTurnStarted event
/// </summary>
public record ChatTurnStartedPayload(
    string ConversationId,
    string ArticleId
);

/// <summary>
/// Payload for ChatTurnComplete event
/// </summary>
public record ChatTurnCompletePayload(
    string ConversationId,
    string ArticleId
);

/// <summary>
/// Payload for ChatMessageStreaming event
/// </summary>
public record ChatMessageStreamingPayload(
    string ConversationId,
    string ArticleId,
    string? Text,
    string? ToolName,
    string? ToolCallId,
    string? ToolDisplay,
    string[]? ToolResultIds,
    bool? IsError,
    string? MessageId,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ChatToolInvoked event
/// </summary>
public record ChatToolInvokedPayload(
    string ConversationId,
    string ArticleId,
    string ToolName,
    string ToolCallId,
    string? ToolDisplay,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ChatToolCompleted event
/// </summary>
public record ChatToolCompletedPayload(
    string ConversationId,
    string ArticleId,
    string ToolCallId,
    string[]? ToolResultIds,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ChatMessageComplete event
/// </summary>
public record ChatMessageCompletePayload(
    string Id,
    string ConversationId,
    string ArticleId,
    string Content,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ChatError event
/// </summary>
public record ChatErrorPayload(
    string ConversationId,
    string ArticleId,
    string Message,
    DateTimeOffset Timestamp
);

/// <summary>
/// Payload for ReceiveMessage event
/// </summary>
public record ReceiveMessagePayload(
    string ArticleId,
    string UserName,
    string Message,
    DateTimeOffset Timestamp
);
