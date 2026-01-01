using System.Text;
using System.Text.Json;
using Amazon.BedrockRuntime;
using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainChatMessage = Medley.Domain.Entities.ChatMessage;

namespace Medley.Application.Services;

/// <summary>
/// Service for AI-powered chat conversations about articles using Microsoft Agent Framework
/// </summary>
public class ArticleChatService : IArticleChatService
{
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<DomainChatMessage> _chatMessageRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatClient _chatClient;
    private readonly ArticleAssistantPlugins _assistantPlugins;
    private readonly ILogger<ArticleChatService> _logger;
    
    // Chat configuration constants
    private const int MaxTokens = 4096;
    private const double Temperature = 0.1;

    public ArticleChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<DomainChatMessage> chatMessageRepository,
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

        // Construct the ChatClient with function invocation support
        _chatClient = bedrockClient.AsIChatClient(bedrockSettings.Value.ModelId);
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

    public async Task<DomainChatMessage> ProcessChatMessageAsync(
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

        // Create the message store for persistence
        var serializedState = JsonSerializer.SerializeToElement(conversationId.ToString("N"));
        var messageStore = new EfChatMessageStore(
            _chatMessageRepository,
            _unitOfWork,
            serializedState);

        // Create the agent with tools and message store factory
        var agent = new ChatClientAgent(
            _chatClient,
            instructions: BuildSystemMessage(conversation.Article),
            tools: [
                AIFunctionFactory.Create(
                    _assistantPlugins.SearchFragmentsAsync,
                    name: "search_fragments",
                    description: "Search for fragments semantically similar to a query string. Returns fragments with similarity scores."
                ),
                AIFunctionFactory.Create(
                    _assistantPlugins.FindSimilarFragmentsAsync,
                    name: "find_similar_to_article",
                    description: "Find fragments semantically similar to the current article content. Useful for finding related content to enhance or expand the article."
                )
            ]
        );

        // Get the latest user message to process
        var latestUserMessage = await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId && m.Role == ChatMessageRole.User)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestUserMessage == null)
        {
            throw new InvalidOperationException($"No user message found for conversation {conversationId}");
        }

        // Create or get existing thread with the message store
        var thread = agent.GetNewThread(messageStore);

        // Run the agent with the user's message
        var result = await agent.RunAsync(
            latestUserMessage.Content,
            thread,
            cancellationToken: cancellationToken);

        // Extract response content
        var responseContent = result.Text ?? string.Empty;

        // Return the most recently saved assistant message from the database
        // (the message store already persisted it during RunAsync)
        var assistantMessage = await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId && m.Role == ChatMessageRole.Assistant)
            .OrderByDescending(m => m.CreatedAt)
            .FirstAsync(cancellationToken);

        _logger.LogInformation("Saved assistant response for conversation {ConversationId}", conversationId);

        return assistantMessage;
    }

    public async Task<List<DomainChatMessage>> GetConversationMessagesAsync(
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
            query = (IOrderedQueryable<DomainChatMessage>)query.Take(limit.Value);
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
