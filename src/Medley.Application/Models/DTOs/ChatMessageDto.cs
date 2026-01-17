using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for ChatMessage entity
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Conversation ID this message belongs to
    /// </summary>
    public required Guid ConversationId { get; set; }

    /// <summary>
    /// User who sent the message (null for assistant/system/tool messages)
    /// </summary>
    public UserSummaryDto? User { get; set; }

    /// <summary>
    /// Role of the message (User, Assistant, System, Tool)
    /// </summary>
    public ChatMessageRole Role { get; set; }

    /// <summary>
    /// The content of the message
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// When this message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Tool calls in this message (if any)
    /// </summary>
    public List<ToolCallDto> ToolCalls { get; set; } = new();
}

/// <summary>
/// Tool call information
/// </summary>
public class ToolCallDto
{
    /// <summary>
    /// Unique identifier for the tool call
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Name of the tool that was called
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Display text for the tool call
    /// </summary>
    public string? Display { get; set; }

    /// <summary>
    /// Arguments passed to the tool (JSON)
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Result from the tool execution
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Indicates if the tool call resulted in an error
    /// </summary>
    public bool IsError { get; set; }
}
