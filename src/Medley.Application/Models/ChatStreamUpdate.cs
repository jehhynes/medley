namespace Medley.Application.Models;

/// <summary>
/// Represents a streaming update during AI agent processing
/// </summary>
public class ChatStreamUpdate
{
    /// <summary>
    /// Type of the streaming update
    /// </summary>
    public required StreamUpdateType Type { get; init; }

    /// <summary>
    /// The conversation ID this update belongs to
    /// </summary>
    public required Guid ConversationId { get; init; }

    /// <summary>
    /// The article ID (for routing SignalR messages)
    /// </summary>
    public Guid? ArticleId { get; init; }

    /// <summary>
    /// Text content for TextDelta and Complete updates
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Tool name for ToolCall and ToolResult updates
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Tool call ID for matching calls with results
    /// </summary>
    public string? ToolCallId { get; init; }

    /// <summary>
    /// Tool message (extracted relevant argument) for ToolCall updates
    /// </summary>
    public string? ToolMessage { get; init; }

    /// <summary>
    /// Message ID for Complete updates (after database save)
    /// </summary>
    public Guid? MessageId { get; init; }

    /// <summary>
    /// Timestamp of the update
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Types of streaming updates
/// </summary>
public enum StreamUpdateType
{
    /// <summary>
    /// Incremental text chunk from the AI model
    /// </summary>
    TextDelta,

    /// <summary>
    /// Agent is invoking a tool/function
    /// </summary>
    ToolCall,

    /// <summary>
    /// Tool/function has completed and returned a result
    /// </summary>
    ToolResult,

    /// <summary>
    /// The entire message from the AI model is complete
    /// </summary>
    MessageComplete,

    /// <summary>
    /// The entire turn/response sequence is complete (e.g. including tool calls and final answer)
    /// </summary>
    TurnComplete
}
