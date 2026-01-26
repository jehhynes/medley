namespace Medley.Application.Hubs.Clients;

/// <summary>
/// Strongly-typed interface for ArticleHub server-to-client methods
/// </summary>
public interface IArticleClient
{
    /// <summary>
    /// Notifies clients that a new article was created
    /// </summary>
    Task ArticleCreated(ArticleCreatedPayload payload);

    /// <summary>
    /// Notifies clients that an article was updated
    /// </summary>
    Task ArticleUpdated(ArticleUpdatedPayload payload);

    /// <summary>
    /// Notifies clients that an article was deleted
    /// </summary>
    Task ArticleDeleted(ArticleDeletedPayload payload);

    /// <summary>
    /// Notifies clients that an article was moved to a different parent
    /// </summary>
    Task ArticleMoved(ArticleMovedPayload payload);

    /// <summary>
    /// Notifies clients that an article's assigned user changed
    /// </summary>
    Task ArticleAssignmentChanged(ArticleAssignmentChangedPayload payload);

    /// <summary>
    /// Notifies clients that a new version was created
    /// </summary>
    Task VersionCreated(VersionCreatedPayload payload);

    /// <summary>
    /// Notifies clients that an improvement plan was generated
    /// </summary>
    Task PlanGenerated(PlanGeneratedPayload payload);

    /// <summary>
    /// Notifies clients that an improvement plan was updated
    /// </summary>
    Task PlanUpdated(PlanUpdatedPayload payload);

    /// <summary>
    /// Notifies clients that a chat turn has started
    /// </summary>
    Task ChatTurnStarted(ChatTurnStartedPayload payload);

    /// <summary>
    /// Notifies clients that a chat turn has completed
    /// </summary>
    Task ChatTurnComplete(ChatTurnCompletePayload payload);

    /// <summary>
    /// Streams chat message content as it's being generated
    /// </summary>
    Task ChatMessageStreaming(ChatMessageStreamingPayload payload);

    /// <summary>
    /// Notifies clients that a tool was invoked during chat
    /// </summary>
    Task ChatToolInvoked(ChatToolInvokedPayload payload);

    /// <summary>
    /// Notifies clients that a tool invocation completed
    /// </summary>
    Task ChatToolCompleted(ChatToolCompletedPayload payload);

    /// <summary>
    /// Notifies clients that a chat message is complete
    /// </summary>
    Task ChatMessageComplete(ChatMessageCompletePayload payload);

    /// <summary>
    /// Notifies clients that a chat error occurred
    /// </summary>
    Task ChatError(ChatErrorPayload payload);

    /// <summary>
    /// Notifies clients that a chat message was received
    /// </summary>
    Task ChatMessageReceived(ChatMessageReceivedPayload payload);

    /// <summary>
    /// Notifies clients that a conversation was completed
    /// </summary>
    Task ConversationCompleted(ConversationCompletedPayload payload);

    /// <summary>
    /// Notifies clients that a conversation was cancelled
    /// </summary>
    Task ConversationCancelled(ConversationCancelledPayload payload);
}
