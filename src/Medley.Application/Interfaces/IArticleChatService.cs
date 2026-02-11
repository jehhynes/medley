using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;

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
        ConversationMode mode = ConversationMode.Agent,
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
    /// Update the mode of an existing conversation
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="mode">The new mode</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateConversationModeAsync(
        Guid conversationId, 
        ConversationMode mode, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a conversation message with streaming updates, using the conversation's mode
    /// </summary>
    /// <param name="conversationId">The conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of streaming updates</returns>
    IAsyncEnumerable<ChatStreamUpdate> ProcessConversationStreamingAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);
}

