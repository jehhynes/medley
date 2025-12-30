using System.Text;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Amazon;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Medley.Application.Services;

/// <summary>
/// Service for AI-powered chat conversations about articles using Semantic Kernel
/// </summary>
public class ArticleChatService : IArticleChatService
{
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<ChatMessage> _chatMessageRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Kernel _kernel;
    private readonly SemanticKernelSettings _settings;
    private readonly ILogger<ArticleChatService> _logger;

    public ArticleChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<ChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IUnitOfWork unitOfWork,
        Kernel kernel,
        IOptions<SemanticKernelSettings> settings,
        ILogger<ArticleChatService> logger)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
        _kernel = kernel;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<ChatConversation> CreateConversationAsync(
        Guid articleId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = new ChatConversation
        {
            ArticleId = articleId,
            State = ConversationState.Active,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _conversationRepository.SaveAsync(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new conversation {ConversationId} for article {ArticleId} by user {UserId}",
            conversation.Id, articleId, userId);

        return conversation;
    }

    public async Task<ChatConversation?> GetActiveConversationAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return await _conversationRepository.Query()
            .Where(c => c.ArticleId == articleId && c.State == ConversationState.Active)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ChatConversation?> GetConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _conversationRepository.Query()
            .Include(c => c.Article)
            .Include(c => c.CreatedBy)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
    }

    public async Task<ChatMessage> ProcessChatMessageAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing chat message for conversation {ConversationId}", conversationId);

        // Load conversation with article
        var conversation = await _conversationRepository.Query()
            .Include(c => c.Article)
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        if (conversation.Article == null)
        {
            throw new InvalidOperationException($"Article not found for conversation {conversationId}");
        }

        // Build chat history with all messages (including the latest user message)
        var chatHistory = await BuildChatHistoryAsync(conversationId, conversation.Article, cancellationToken);

        // Get chat completion service from kernel
        var chatCompletion = _kernel.GetRequiredService<IChatCompletionService>();

        var executionSettings = new AmazonClaudeExecutionSettings
        {
            MaxTokensToSample = _settings.MaxTokens,
            Temperature = (float)_settings.Temperature
        };

        // Get AI response
        var result = await chatCompletion.GetChatMessageContentAsync(
            chatHistory,
            executionSettings: executionSettings,
            kernel: _kernel,
            cancellationToken: cancellationToken);

        var responseContent = result.Content ?? string.Empty;

        // Save assistant message
        var assistantMessage = new ChatMessage
        {
            ConversationId = conversationId,
            UserId = null, // Assistant messages don't have a user
            MessageType = ChatMessageType.Assistant,
            Content = responseContent,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _chatMessageRepository.SaveAsync(assistantMessage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved assistant response for conversation {ConversationId}", conversationId);

        return assistantMessage;
    }

    private async Task<ChatHistory> BuildChatHistoryAsync(
        Guid conversationId,
        Article article,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new ChatHistory();

        // Add system message with article context
        var systemMessage = BuildSystemMessage(article);
        chatHistory.AddSystemMessage(systemMessage);

        // Load recent messages from conversation
        var messages = await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Take(_settings.MaxHistoryMessages)
            .Include(m => m.User)
            .ToListAsync(cancellationToken);

        // Convert to Semantic Kernel ChatHistory format
        foreach (var msg in messages)
        {
            switch (msg.MessageType)
            {
                case ChatMessageType.User:
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case ChatMessageType.Assistant:
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
                case ChatMessageType.ToolCall:
                    // Tool calls can be handled as needed in the future
                    _logger.LogWarning("ToolCall message type not yet implemented for conversation {ConversationId}", conversationId);
                    break;
            }
        }

        return chatHistory;
    }

    public async Task<List<ChatMessage>> GetConversationMessagesAsync(
        Guid conversationId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId)
            .Include(m => m.User)
            .OrderBy(m => m.CreatedAt);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<ChatMessage>)query.Take(limit.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<ChatConversation>> GetConversationHistoryAsync(
        Guid articleId,
        CancellationToken cancellationToken = default)
    {
        return await _conversationRepository.Query()
            .Where(c => c.ArticleId == articleId &&
                       (c.State == ConversationState.Complete || c.State == ConversationState.Cancelled))
            .Include(c => c.Messages)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        conversation.State = ConversationState.Complete;
        conversation.CompletedAt = DateTimeOffset.UtcNow;

        await _conversationRepository.SaveAsync(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed conversation {ConversationId}", conversationId);
    }

    public async Task CancelConversationAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        conversation.State = ConversationState.Cancelled;

        await _conversationRepository.SaveAsync(conversation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled conversation {ConversationId}", conversationId);
    }

    private string BuildSystemMessage(Article article)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"You are an AI assistant helping users with the article \"{article.Title}\".");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(article.Content))
        {
            sb.AppendLine("Current article content:");
            sb.AppendLine(article.Content);
            sb.AppendLine();
        }

        sb.AppendLine("Help the user improve, expand, or answer questions about this article. Be concise and helpful.");

        return sb.ToString();
    }
}

