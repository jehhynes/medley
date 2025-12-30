namespace Medley.Domain.Enums;

/// <summary>
/// Type of message in a chat conversation
/// </summary>
public enum ChatMessageType
{
    /// <summary>
    /// Message from the user
    /// </summary>
    User = 0,
    
    /// <summary>
    /// Message from the AI assistant
    /// </summary>
    Assistant = 1,
    
    /// <summary>
    /// Tool call message (function/action invocation)
    /// </summary>
    ToolCall = 2
}

