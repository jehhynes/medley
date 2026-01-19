using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Detailed conversation information (for GetConversation endpoint)
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// The mode of this conversation
    /// </summary>
    public required string Mode { get; set; }

    /// <summary>
    /// Indicates if the conversation is currently running (AI is processing)
    /// </summary>
    public required bool IsRunning { get; set; }

    /// <summary>
    /// When this conversation was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// User ID who created this conversation
    /// </summary>
    public required Guid CreatedBy { get; set; }

    /// <summary>
    /// The plan this conversation is implementing (if applicable)
    /// </summary>
    public Guid? ImplementingPlanId { get; set; }

    /// <summary>
    /// Version of the plan being implemented (if applicable)
    /// </summary>
    public int? ImplementingPlanVersion { get; set; }
}

/// <summary>
/// Conversation history item (for GetHistory endpoint)
/// </summary>
public class ConversationHistoryItemDto
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public required string State { get; set; }

    /// <summary>
    /// When this conversation was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Number of messages in the conversation
    /// </summary>
    public required int MessageCount { get; set; }

    /// <summary>
    /// When this conversation was completed (if applicable)
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>
/// Summary information about a conversation (for nested references)
/// </summary>
public class ConversationSummaryDto
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Indicates if the conversation is currently running
    /// </summary>
    public required bool IsRunning { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public required ConversationState State { get; set; }
}

/// <summary>
/// Response after sending a message
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// ID of the created message
    /// </summary>
    public required Guid MessageId { get; set; }

    /// <summary>
    /// ID of the conversation
    /// </summary>
    public required Guid ConversationId { get; set; }
}

/// <summary>
/// Response after completing or cancelling a conversation
/// </summary>
public class ConversationStatusResponse
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public required ConversationState State { get; set; }

    /// <summary>
    /// Timestamp of the status change
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }
}