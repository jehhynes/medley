namespace Medley.Domain.Enums;

/// <summary>
/// Mode of a chat conversation
/// </summary>
public enum ConversationMode
{
    /// <summary>
    /// Standard chat mode for asking questions and general assistance
    /// </summary>
    Chat = 0,

    /// <summary>
    /// Plan mode for generating article improvement plans
    /// </summary>
    Plan = 1
}
