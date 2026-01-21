using Hangfire;
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
        _logger.LogInformation("Processing chat message {MessageId}", userMessageId);

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
                    _logger.LogWarning("User message {MessageId} not found", userMessageId);
                    return;
                }

                var conversation = userMessage.Conversation;
                if (conversation == null)
                {
                    _logger.LogWarning("Conversation not found for message {MessageId}", userMessageId);
                    return;
                }

                var article = conversation.Article;
                if (article == null)
                {
                    _logger.LogWarning("Article not found for conversation {ConversationId}", conversation.Id);
                    await SendErrorNotification(conversation.Id, "Article not found", cancellationToken);
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
                _logger.LogInformation("Requesting AI response with streaming for conversation {ConversationId}", conversation.Id);
                
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
                                    MessageId = update.MessageId ?? Guid.Empty,
                                    Timestamp = update.Timestamp
                                });
                            break;

                        case Models.StreamUpdateType.MessageComplete:
                            // Send final complete message
                            _logger.LogInformation("Chat message processed successfully for conversation {ConversationId}, response saved with ID {MessageId}",
                                conversation.Id, update.MessageId);

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

                // Get the created plan and send PlanGenerated event
                if (conversation.Mode == ConversationMode.Plan)
                {
                    var plan = await _planRepository.Query()
                        .Where(p => p.ArticleId == conversation.ArticleId && p.Status == PlanStatus.Draft)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (plan != null)
                    {
                        _logger.LogInformation("Sending PlanGenerated event for plan {PlanId}, article {ArticleId}",
                            plan.Id, conversation.ArticleId);

                        await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                            .PlanGenerated(new PlanGeneratedPayload
                            {
                                ArticleId = conversation.ArticleId,
                                PlanId = plan.Id,
                                Timestamp = DateTimeOffset.UtcNow
                            });
                    }
                }

                // Get the created AI version and send VersionCreated event
                if (conversation.Mode == ConversationMode.Agent)
                {
                    var aiVersion = await _versionRepository.Query()
                        .Where(v => v.ArticleId == conversation.ArticleId 
                            && v.ConversationId == conversation.Id 
                            && v.VersionType == VersionType.AI)
                        .OrderByDescending(v => v.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (aiVersion != null)
                    {
                        _logger.LogInformation("Sending VersionCreated event for AI version {VersionId}, article {ArticleId}",
                            aiVersion.Id, conversation.ArticleId);

                        await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                            .VersionCreated(new VersionCreatedPayload
                            {
                                ArticleId = conversation.ArticleId,
                                VersionId = aiVersion.Id,
                                VersionNumber = aiVersion.VersionNumber.ToString(),
                                VersionType = VersionType.AI,
                                CreatedAt = aiVersion.CreatedAt
                            });
                    }
                }

                conversation.IsRunning = false;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message {MessageId}", userMessageId);
            
            // Try to get conversation ID for error notification
            var userMessage = await _chatMessageRepository.Query()
                .Select(m => new { m.Id, m.ConversationId })
                .FirstOrDefaultAsync(m => m.Id == userMessageId, cancellationToken);
            
            if (userMessage != null)
            {
                await SendErrorNotification(userMessage.ConversationId, ex.Message, cancellationToken);
            }
            
            throw; // Re-throw for Hangfire retry logic
        }
    }

    /// <summary>
    /// Send error notification via SignalR
    /// </summary>
    private async Task SendErrorNotification(Guid conversationId, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            // Get conversation to find article ID for the group
            var conversation = await _chatService.GetConversationAsync(conversationId, cancellationToken);
            if (conversation == null)
            {
                _logger.LogWarning("Cannot send error notification - conversation {ConversationId} not found", conversationId);
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
            _logger.LogError(ex, "Failed to send error notification for conversation {ConversationId}", conversationId);
        }
    }

}

