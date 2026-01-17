using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for ChatConversation entity
/// </summary>
public class ConversationDto
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article ID this conversation is about
    /// </summary>
    public required Guid ArticleId { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public ConversationState State { get; set; }

    /// <summary>
    /// The mode of this conversation
    /// </summary>
    public ConversationMode Mode { get; set; }

    /// <summary>
    /// The plan this conversation is implementing (if applicable)
    /// </summary>
    public Guid? ImplementingPlanId { get; set; }

    /// <summary>
    /// Indicates if the conversation is currently running (AI is processing)
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// When this conversation was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this conversation was completed (if applicable)
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// User who created this conversation
    /// </summary>
    public required UserSummaryDto CreatedBy { get; set; }

    /// <summary>
    /// Messages in this conversation
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();
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
    public bool IsRunning { get; set; }

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public ConversationState State { get; set; }
}
