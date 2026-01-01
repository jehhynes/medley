using Hangfire;
using Hangfire.MissionControl;
using Hangfire.Server;
using Medley.Application.Constants;
using Medley.Application.Hubs;
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
[MissionLauncher]
public class ArticleChatJob : BaseHangfireJob<ArticleChatJob>
{
    private readonly IArticleChatService _chatService;
    private readonly IRepository<ChatMessage> _chatMessageRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IHubContext<ArticleHub> _hubContext;

    public ArticleChatJob(
        IArticleChatService chatService,
        IRepository<ChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IRepository<Plan> planRepository,
        IHubContext<ArticleHub> hubContext,
        IUnitOfWork unitOfWork,
        ILogger<ArticleChatJob> logger) : base(unitOfWork, logger)
    {
        _chatService = chatService;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _planRepository = planRepository;
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
    [Mission]
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
                    .SendAsync("ChatMessageProcessing", new
                    {
                        conversationId = conversation.Id.ToString(),
                        messageId = userMessageId.ToString(),
                        timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);

                // Process AI response
                _logger.LogInformation("Requesting AI response for conversation {ConversationId}", conversation.Id);
                var assistantMessage = await _chatService.ProcessChatMessageAsync(
                    conversation.Id,
                    cancellationToken);

                if (assistantMessage == null || string.IsNullOrEmpty(assistantMessage.Content))
                {
                    _logger.LogWarning("Empty AI response for conversation {ConversationId}", conversation.Id);
                    await SendErrorNotification(conversation.Id, "Empty response from AI", cancellationToken);
                    return;
                }

                _logger.LogInformation("Chat message processed successfully for conversation {ConversationId}, response saved with ID {MessageId}",
                    conversation.Id, assistantMessage.Id);

                // Notify UI that processing is complete with full message
                await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                    .SendAsync("ChatMessageComplete", new
                    {
                        id = assistantMessage.Id.ToString(),
                        conversationId = conversation.Id.ToString(),
                        role = "assistant",
                        content = assistantMessage.Content,
                        userName = ChatConstants.AssistantDisplayName,
                        createdAt = assistantMessage.CreatedAt,
                        articleId = conversation.ArticleId.ToString()
                    }, cancellationToken);
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
                .SendAsync("ChatError", new
                {
                    conversationId = conversationId.ToString(),
                    error = errorMessage,
                    timestamp = DateTimeOffset.UtcNow
                }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error notification for conversation {ConversationId}", conversationId);
        }
    }

    /// <summary>
    /// Process plan generation message and send notification when plan is created
    /// </summary>
    [Queue("ui")]
    [DisableMultipleQueuedItemsFilter]
    [Mission]
    public async Task ProcessPlanGenerationAsync(
        Guid userMessageId,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing plan generation for message {MessageId}, conversation {ConversationId}", 
            userMessageId, conversationId);

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
                    .SendAsync("ChatMessageProcessing", new
                    {
                        conversationId = conversation.Id.ToString(),
                        messageId = userMessageId.ToString(),
                        timestamp = DateTimeOffset.UtcNow
                    }, cancellationToken);

                // Process plan generation
                _logger.LogInformation("Generating plan for conversation {ConversationId}", conversation.Id);
                var assistantMessage = await _chatService.ProcessPlanGenerationAsync(
                    conversation.Id,
                    cancellationToken);

                if (assistantMessage == null || string.IsNullOrEmpty(assistantMessage.Content))
                {
                    _logger.LogWarning("Empty AI response for plan generation {ConversationId}", conversation.Id);
                    await SendErrorNotification(conversation.Id, "Empty response from AI", cancellationToken);
                    return;
                }

                _logger.LogInformation("Plan generation processed successfully for conversation {ConversationId}", conversation.Id);

                // Notify UI that processing is complete with full message
                await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                    .SendAsync("ChatMessageComplete", new
                    {
                        id = assistantMessage.Id.ToString(),
                        conversationId = conversation.Id.ToString(),
                        role = "assistant",
                        content = assistantMessage.Content,
                        userName = ChatConstants.AssistantDisplayName,
                        createdAt = assistantMessage.CreatedAt,
                        articleId = conversation.ArticleId.ToString()
                    }, cancellationToken);

                // Get the created plan and send PlanGenerated event
                var plan = await _planRepository.Query()
                    .Where(p => p.ArticleId == conversation.ArticleId && p.Status == PlanStatus.Draft)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (plan != null)
                {
                    _logger.LogInformation("Sending PlanGenerated event for plan {PlanId}, article {ArticleId}", 
                        plan.Id, conversation.ArticleId);

                    await _hubContext.Clients.Group($"Article_{conversation.ArticleId}")
                        .SendAsync("PlanGenerated", new
                        {
                            articleId = conversation.ArticleId.ToString(),
                            planId = plan.Id.ToString(),
                            conversationId = conversation.Id.ToString(),
                            timestamp = DateTimeOffset.UtcNow
                        }, cancellationToken);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing plan generation for message {MessageId}", userMessageId);
            await SendErrorNotification(conversationId, ex.Message, cancellationToken);
            throw; // Re-throw for Hangfire retry logic
        }
    }
}

