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
    /// Conversation has been archived (e.g., planning conversation after plan acceptance)
    /// </summary>
    Archived = 2
}

