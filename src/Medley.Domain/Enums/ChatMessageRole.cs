namespace Medley.Domain.Enums;

/// <summary>
/// Role/type of message in a chat conversation (matches Microsoft.Extensions.AI ChatRole)
/// </summary>
public enum ChatMessageRole
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
    /// System message (instructions, context)
    /// </summary>
    System = 2,
    
    /// <summary>
    /// Tool call message (function/action invocation or result)
    /// </summary>
    Tool = 3
}
