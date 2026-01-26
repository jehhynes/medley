using Medley.Domain.Enums;

namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Payload for ArticleCreated event
/// </summary>
public record ArticleCreatedPayload
{
    public required Guid ArticleId { get; init; }
    public required string Title { get; init; }
    public Guid? ParentArticleId { get; init; }
    public Guid? ArticleTypeId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleUpdated event
/// </summary>
public record ArticleUpdatedPayload
{
    public required Guid ArticleId { get; init; }
    public required string Title { get; init; }
    public Guid? ArticleTypeId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleDeleted event
/// </summary>
public record ArticleDeletedPayload
{
    public required Guid ArticleId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleMoved event
/// </summary>
public record ArticleMovedPayload
{
    public required Guid ArticleId { get; init; }
    public Guid? OldParentId { get; init; }
    public Guid? NewParentId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ArticleAssignmentChanged event
/// </summary>
public record ArticleAssignmentChangedPayload
{
    public required Guid ArticleId { get; init; }
    public Guid? UserId { get; init; }
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
    public required Guid ArticleId { get; init; }
    public required Guid VersionId { get; init; }
    public required string VersionNumber { get; init; }
    public required VersionType VersionType { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Payload for PlanGenerated event
/// </summary>
public record PlanGeneratedPayload
{
    public required Guid ArticleId { get; init; }
    public required Guid PlanId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for PlanUpdated event
/// </summary>
public record PlanUpdatedPayload
{
    public required Guid ArticleId { get; init; }
    public required Guid PlanId { get; init; }
    public required int FragmentsAdded { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatTurnStarted event
/// </summary>
public record ChatTurnStartedPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
}

/// <summary>
/// Payload for ChatTurnComplete event
/// </summary>
public record ChatTurnCompletePayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
}

/// <summary>
/// Payload for ChatMessageStreaming event
/// </summary>
public record ChatMessageStreamingPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required ChatMessageRole Role { get; init; }
    public string? Text { get; init; }
    //public string? ToolName { get; init; }
    //public string? ToolCallId { get; init; }
    //public string? ToolDisplay { get; init; }
    //public Guid[]? ToolResultIds { get; init; }
    public bool? IsError { get; init; }
    public Guid? MessageId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatToolInvoked event
/// </summary>
public record ChatToolInvokedPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required string ToolName { get; init; }
    public required string ToolCallId { get; init; }
    public string? ToolDisplay { get; init; }
    public required Guid MessageId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatToolCompleted event
/// </summary>
public record ChatToolCompletedPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required string ToolCallId { get; init; }
    public Guid[]? ToolResultIds { get; init; }
    public required bool IsError { get; init; }
    public required Guid MessageId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatMessageComplete event
/// </summary>
public record ChatMessageCompletePayload
{
    public required Guid MessageId { get; init; }
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required ChatMessageRole Role { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatError event
/// </summary>
public record ChatErrorPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Payload for ChatMessageReceived event
/// </summary>
public record ChatMessageReceivedPayload
{
    public required Guid MessageId { get; init; }
    public required Guid ConversationId { get; init; }
    public required ChatMessageRole Role { get; init; }
    public required string Text { get; init; }
    public required string UserName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required Guid ArticleId { get; init; }
}

/// <summary>
/// Payload for ConversationCompleted event
/// </summary>
public record ConversationCompletedPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}

/// <summary>
/// Payload for ConversationCancelled event
/// </summary>
public record ConversationCancelledPayload
{
    public required Guid ConversationId { get; init; }
    public required Guid ArticleId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}