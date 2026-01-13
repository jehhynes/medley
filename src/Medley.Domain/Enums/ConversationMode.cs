namespace Medley.Domain.Enums;

/// <summary>
/// Mode of a chat conversation
/// </summary>
public enum ConversationMode
{
    /// <summary>
    /// Agent mode for AI assistance, Q&A, and implementing plans
    /// </summary>
    Agent = 0,

    /// <summary>
    /// Plan mode for generating article improvement plans
    /// </summary>
    Plan = 1
}
