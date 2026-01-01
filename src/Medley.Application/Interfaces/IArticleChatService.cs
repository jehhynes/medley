using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for AI-powered chat conversations about articles
/// </summary>
public interface IArticleChatService
{
    /// <summary>
    /// Create a new conversation for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="userId">The user creating the conversation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created conversation</returns>
    Task<ChatConversation> CreateConversationAsync(
        Guid articleId, 
        Guid userId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the active conversation for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active conversation or null if none exists</returns>
    Task<ChatConversation?> GetActiveConversationAsync(
        Guid articleId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a conversation by ID
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The conversation or null if not found</returns>
    Task<ChatConversation?> GetConversationAsync(
        Guid conversationId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a chat message and return the saved assistant message
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved assistant message entity</returns>
    Task<ChatMessage> ProcessChatMessageAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages for a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="limit">Maximum number of messages to return (null for all)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of messages ordered by creation date</returns>
    Task<List<ChatMessage>> GetConversationMessagesAsync(
        Guid conversationId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of past conversations for an article
    /// </summary>
    /// <param name="articleId">The article ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of completed/cancelled conversations</returns>
    Task<List<ChatConversation>> GetConversationHistoryAsync(
        Guid articleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a conversation as complete
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CompleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a plan generation message (similar to chat but with plan-specific tools and template)
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved assistant message entity</returns>
    Task<ChatMessage> ProcessPlanGenerationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}

