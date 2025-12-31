using System.Text;
using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Medley.Application.Services;

/// <summary>
/// Service for AI-powered chat conversations about articles using Microsoft.Extensions.AI
/// </summary>
public class ArticleChatService : IArticleChatService
{
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Medley.Domain.Entities.ChatMessage> _chatMessageRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatClient _chatClient;
    private readonly ArticleAssistantPlugins _assistantPlugins;
    private readonly ILogger<ArticleChatService> _logger;
    
    // Chat configuration constants
    private const int MaxTokens = 4096;
    private const double Temperature = 0.1;
    private const int MaxHistoryMessages = 20;

    public ArticleChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<Medley.Domain.Entities.ChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IUnitOfWork unitOfWork,
        AmazonBedrockRuntimeClient bedrockClient,
        ArticleAssistantPlugins assistantPlugins,
        IOptions<BedrockSettings> bedrockSettings,
        ILogger<ArticleChatService> logger)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
        _assistantPlugins = assistantPlugins;
        _logger = logger;

        // Construct the ChatClient directly
        _chatClient = bedrockClient.AsIChatClient(bedrockSettings.Value.ModelId);

        //Wrap the chat client to enable function invocation
        _chatClient = new ChatClientBuilder(_chatClient).UseFunctionInvocation().Build();
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

    public async Task<Domain.Entities.ChatMessage> ProcessChatMessageAsync(
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

        // Configure chat options with tools from ArticleAssistantPlugins
        var chatOptions = new ChatOptions
        {
            MaxOutputTokens = MaxTokens,
            Temperature = (float)Temperature,
            // Add tools for function calling using AIFunctionFactory
            Tools = [
                AIFunctionFactory.Create(
                    _assistantPlugins.SearchFragmentsAsync,
                    "search_fragments",
                    "Search for fragments semantically similar to a query string. Returns fragments with similarity scores."
                ),
                AIFunctionFactory.Create(
                    _assistantPlugins.FindSimilarFragmentsAsync,
                    "find_similar_to_article",
                    "Find fragments semantically similar to the current article content. Useful for finding related content to enhance or expand the article."
                )
            ]
        };

        // Get AI response
        var response = await _chatClient.GetResponseAsync(
            chatHistory,
            chatOptions,
            cancellationToken);

        var responseContent = response.Text ?? string.Empty;

        // Save assistant message
        var assistantMessage = new Domain.Entities.ChatMessage
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

    private async Task<List<Microsoft.Extensions.AI.ChatMessage>> BuildChatHistoryAsync(
        Guid conversationId,
        Article article,
        CancellationToken cancellationToken = default)
    {
        var chatHistory = new List<Microsoft.Extensions.AI.ChatMessage>();

        // Add system message with article context
        var systemMessage = BuildSystemMessage(article);
        chatHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemMessage));

        // Load recent messages from conversation
        var messages = await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .Take(MaxHistoryMessages)
            .Include(m => m.User)
            .ToListAsync(cancellationToken);

        // Convert to Microsoft.Extensions.AI ChatMessage format
        foreach (var msg in messages)
        {
            switch (msg.MessageType)
            {
                case ChatMessageType.User:
                    chatHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, msg.Content));
                    break;
                case ChatMessageType.Assistant:
                    chatHistory.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, msg.Content));
                    break;
                case ChatMessageType.ToolCall:
                    // Tool calls can be handled as needed in the future
                    _logger.LogWarning("ToolCall message type not yet implemented for conversation {ConversationId}", conversationId);
                    break;
            }
        }

        return chatHistory;
    }

    public async Task<List<Domain.Entities.ChatMessage>> GetConversationMessagesAsync(
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
            query = (IOrderedQueryable<Domain.Entities.ChatMessage>)query.Take(limit.Value);
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

