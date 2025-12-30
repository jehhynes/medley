namespace Medley.Domain.Enums;

/// <summary>
/// State of a chat conversation
/// </summary>
public enum ConversationState
{
    /// <summary>
    /// Conversation is active and ongoing
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Conversation has been completed (e.g., draft accepted)
    /// </summary>
    Complete = 1,
    
    /// <summary>
    /// Conversation was cancelled by the user
    /// </summary>
    Cancelled = 2
}

