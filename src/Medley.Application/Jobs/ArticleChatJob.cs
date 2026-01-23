using Hangfire;
using Hangfire.Console;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Helpers;
using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Medley.Application.Jobs;

/// <summary>
/// Background job for processing AI chat messages asynchronously
/// </summary>
public class ArticleChatJob : BaseHangfireJob<ArticleChatJob>
{
    private readonly IArticleChatService _chatService;
    private readonly IRepository<ChatMessage> _chatMessageRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<ArticleVersion> _versionRepository;
    private readonly IHubContext<ArticleHub, IArticleClient> _hubContext;

    public ArticleChatJob(
        IArticleChatService chatService,
        IRepository<ChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IRepository<ArticleVersion> versionRepository,
        IHubContext<ArticleHub, IArticleClient> hubContext,
        IUnitOfWork unitOfWork,
        ILogger<ArticleChatJob> logger) : base(unitOfWork, logger)
    {
        _chatService = chatService;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
        _versionRepository = versionRepository;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Process a chat message and send AI response via SignalR
    /// </summary>
    /// <param name="userMessageId">ID of the saved user message</param>
    /// <param name="context">Hangfire perform context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [Queue("ui")]
    [DisableMultipleQueuedItemsFilter]
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessChatMessageAsync(
        Guid userMessageId,
        PerformContext context,
        CancellationToken cancellationToken)
    {
        LogInfo(context, $"Processing chat message {userMessageId}");

        try
        {
            await ExecuteWithTransactionAsync(async () =>
            {
                // Load the user message
                var userMessage = await _chatMessageRepository.Query()
                    .Include(m => m.Conversation)
                        .ThenInclude(c => c.Article)
                    .FirstOrDefaultAsync(m => m.Id == userMessageId, cancellationToken);

                if (userMessage == null)
                {
                    LogWarning(context, $"User message {userMessageId} not found");
                    return;
                }

                var conversation = userMessage.Conversation;
                if (conversation == null)
                {
                    LogWarning(context, $"Conversation not found for message {userMessageId}");
                    return;
                }

                var article = conversation.Article;
                if (article == null)
                {
                    LogWarning(context, $"Article not found for conversation {conversation.Id}");
                    await SendErrorNotification(conversation.Id, "Article not found", context, cancellationToken);
                    return;
                }

                // Notify UI that processing has started
                await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                    .ChatTurnStarted(new ChatTurnStartedPayload
                    {
                        ConversationId = conversation.Id,
                        ArticleId = conversation.ArticleId
                    });

                // Process AI response with streaming
                LogInfo(context, $"Requesting AI response with streaming for conversation {conversation.Id}");
                
                await foreach (var update in _chatService.ProcessConversationStreamingAsync(
                    conversation.Id,
                    cancellationToken))
                {
                    switch (update.Type)
                    {
                        case Models.StreamUpdateType.TextDelta:
                            // Send incremental text updates
                            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                                .ChatMessageStreaming(new ChatMessageStreamingPayload
                                {
                                    ConversationId = update.ConversationId,
                                    ArticleId = conversation.ArticleId,
                                    Role = ChatMessageRole.Assistant,
                                    Text = update.Text,
                                    MessageId = update.MessageId,
                                    Timestamp = update.Timestamp
                                });
                            break;

                        case Models.StreamUpdateType.ToolCall:
                            // Notify about tool invocations
                            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                                .ChatToolInvoked(new ChatToolInvokedPayload
                                {
                                    ConversationId = update.ConversationId,
                                    ArticleId = conversation.ArticleId,
                                    ToolName = update.ToolName ?? string.Empty,
                                    ToolCallId = update.ToolCallId ?? string.Empty,
                                    ToolDisplay = update.ToolDisplay,
                                    MessageId = update.MessageId ?? Guid.Empty,
                                    Timestamp = update.Timestamp
                                });
                            break;

                        case Models.StreamUpdateType.ToolResult:
                            // Notify about tool results
                            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                                .ChatToolCompleted(new ChatToolCompletedPayload
                                {
                                    ConversationId = update.ConversationId,
                                    ArticleId = conversation.ArticleId,
                                    ToolCallId = update.ToolCallId ?? string.Empty,
                                    ToolResultIds = update.ToolResultIds?.ToArray(),
                                    IsError = update.IsError ?? false,
                                    MessageId = update.MessageId ?? Guid.Empty,
                                    Timestamp = update.Timestamp
                                });
                            break;

                        case Models.StreamUpdateType.MessageComplete:
                            // Send final complete message
                            LogSuccess(context, $"Chat message processed successfully for conversation {conversation.Id}");

                            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                                .ChatMessageComplete(new ChatMessageCompletePayload
                                {
                                    MessageId = update.MessageId ?? Guid.Empty,
                                    ConversationId = conversation.Id,
                                    ArticleId = conversation.ArticleId,
                                    Role = ChatMessageRole.Assistant,
                                    Content = update.Text ?? string.Empty,
                                    Timestamp = update.Timestamp
                                });
                            break;

                        case Models.StreamUpdateType.TurnComplete:
                            // Send turn complete signal
                            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                                .ChatTurnComplete(new ChatTurnCompletePayload
                                {
                                    ConversationId = conversation.Id,
                                    ArticleId = conversation.ArticleId
                                });
                            break;
                    }
                }

                conversation.IsRunning = false;
            });
        }
        catch (Exception ex)
        {
            LogError(context, ex, $"Error processing chat message {userMessageId}");
            
            // Try to get conversation ID for error notification
            var userMessage = await _chatMessageRepository.Query()
                .Select(m => new { m.Id, m.ConversationId })
                .FirstOrDefaultAsync(m => m.Id == userMessageId, cancellationToken);
            
            if (userMessage != null)
            {
                await SendErrorNotification(userMessage.ConversationId, ex.Message, context, cancellationToken);
            }
            
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Send error notification via SignalR
    /// </summary>
    private async Task SendErrorNotification(Guid conversationId, string errorMessage, PerformContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Get conversation to find article ID for the group
            var conversation = await _chatService.GetConversationAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                LogWarning(context, $"Cannot send error notification - conversation {conversationId} not found");
                return;
            }

            await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                .ChatError(new ChatErrorPayload
                {
                    ConversationId = conversationId,
                    ArticleId = conversation.ArticleId,
                    Message = errorMessage,
                    Timestamp = DateTimeOffset.UtcNow
                });
        }
        catch (Exception ex)
        {
            LogError(context, ex, $"Failed to send error notification for conversation {conversationId}");
        }
    }

}

