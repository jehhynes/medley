using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Medley.Application.Interfaces;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
    private readonly IRepository<Template> _templateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatClient _chatClient;
    private readonly ArticleChatToolsFactory _pluginsFactory;
    private readonly ILogger<ArticleChatService> _logger;
    private readonly AiCallContext _aiCallContext;
    
    // Chat configuration constants
    private const int MaxTokens = 4096;
    private const double Temperature = 0.1;

    public ArticleChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<DomainChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IRepository<Template> templateRepository,
        IUnitOfWork unitOfWork,
        IChatClient chatClient,
        ArticleChatToolsFactory pluginsFactory,
        ILogger<ArticleChatService> logger,
        AiCallContext aiCallContext)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
        _pluginsFactory = pluginsFactory;
        _logger = logger;
        _aiCallContext = aiCallContext;

        // Wrap the provided chat client with function invocation support
        _chatClient = new ChatClientBuilder(chatClient).UseFunctionInvocation().Build();
    }

    public async Task<ChatConversation> CreateConversationAsync(
        Guid articleId,
        Guid userId,
        ConversationMode mode = ConversationMode.Chat,
        CancellationToken cancellationToken = default)
    {
        var conversation = new ChatConversation
        {
            ArticleId = articleId,
            State = ConversationState.Active,
            Mode = mode,
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


    public async IAsyncEnumerable<ChatStreamUpdate> ProcessConversationStreamingAsync(
        Guid conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using (_aiCallContext.SetContext(nameof(ArticleChatService), nameof(ProcessConversationStreamingAsync), nameof(ChatConversation), conversationId))
        {
            _logger.LogInformation("Processing conversation with streaming for conversation {ConversationId}", conversationId);
            
            // Load conversation with article
            var (conversation, article) = await LoadConversationWithArticleAsync(conversationId, true, cancellationToken);
            
            // Create article-scoped plugins instance
            var assistantPlugins = _pluginsFactory.Create(article.Id);
            
            // Set user ID in plugins for potential plan creation
            assistantPlugins.SetCurrentUserId(conversation.CreatedByUserId);
            
            // Select system prompt and tools based on mode
            string systemMessage;
            AIFunction[] tools;
            
            if (conversation.Mode == ConversationMode.Plan)
            {
                // Load the ArticleImprovementPlan template
                var template = await _templateRepository.Query()
                    .FirstOrDefaultAsync(t => t.Type == TemplateType.ArticleImprovementPlan, cancellationToken);
                
                if (template == null)
                {
                    throw new InvalidOperationException("Article Improvement Plan template not found");
                }
                
                systemMessage = template.Content.Replace("{article.Title}", article.Title);
                tools = CreateTools(assistantPlugins, conversation.Mode);
            }
            else
            {
                // Load the ArticleChat template or fall back to code-based system message
                var template = await _templateRepository.Query()
                    .FirstOrDefaultAsync(t => t.Type == TemplateType.ArticleChat, cancellationToken);
                
                if (template != null)
                {
                    systemMessage = template.Content.Replace("{article.Title}", article.Title)
                                                  .Replace("{article.Content}", article.Content ?? string.Empty);
                }
                else
                {
                    throw new InvalidDataException("Article Chat template not found");
                }
                
                tools = CreateTools(assistantPlugins, conversation.Mode);
            }
            
            // Create the message store for persistence
            var messageStore = CreateMessageStore(conversationId);
            
            // Create the agent with appropriate system message and tools
            var agent = CreateChatAgent(systemMessage, tools);
            
            // Get the latest user message to process
            var latestUserMessage = await GetLatestUserMessageAsync(conversationId, cancellationToken);
            
            // Create or get existing thread with the message store
            var thread = agent.GetNewThread(messageStore);
            
            // Stream the agent response
            var agentUpdates = agent.RunStreamingAsync(
                latestUserMessage.Content,
                thread,
                cancellationToken: cancellationToken);
            
            // Process and yield streaming updates
            await foreach (var update in ProcessStreamingUpdatesAsync(
                agentUpdates,
                conversationId,
                conversation.ArticleId,
                cancellationToken))
            {
                yield return update;
            }
            
            // Retrieve the most recent assistant message
            var assistantMessage = await GetLatestAssistantMessageAsync(conversationId, cancellationToken);
            
            _logger.LogInformation("Completed streaming for conversation {ConversationId} (Mode: {Mode}), message {MessageId}",
                conversationId, conversation.Mode, assistantMessage.Id);
            
            // Yield final complete update
            yield return CreateCompletionUpdate(assistantMessage, conversationId, conversation.ArticleId);
        }
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

    public async Task UpdateConversationModeAsync(
        Guid conversationId,
        ConversationMode mode,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        if (conversation.Mode != mode)
        {
            _logger.LogInformation("Updating conversation {ConversationId} mode from {OldMode} to {NewMode}",
                conversationId, conversation.Mode, mode);
            
            conversation.Mode = mode;
            await _conversationRepository.SaveAsync(conversation);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }


    #region Helper Methods

    /// <summary>
    /// Loads a conversation with its associated article and validates both exist.
    /// </summary>
    private async Task<(ChatConversation conversation, Article article)> LoadConversationWithArticleAsync(
        Guid conversationId,
        bool includeCreatedBy = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ChatConversation> query = _conversationRepository.Query()
            .Include(c => c.Article);

        if (includeCreatedBy)
        {
            query = query.Include(c => c.CreatedBy);
        }

        var conversation = await query.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        if (conversation.Article == null)
        {
            throw new InvalidOperationException($"Article not found for conversation {conversationId}");
        }

        return (conversation, conversation.Article);
    }

    /// <summary>
    /// Retrieves the latest user message for a conversation.
    /// </summary>
    private async Task<DomainChatMessage> GetLatestUserMessageAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var latestUserMessage = await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId && m.Role == ChatMessageRole.User)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestUserMessage == null)
        {
            throw new InvalidOperationException($"No user message found for conversation {conversationId}");
        }

        return latestUserMessage;
    }

    /// <summary>
    /// Retrieves the most recent assistant message for a conversation.
    /// </summary>
    private async Task<DomainChatMessage> GetLatestAssistantMessageAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        return await _chatMessageRepository.Query()
            .Where(m => m.ConversationId == conversationId && m.Role == ChatMessageRole.Assistant)
            .OrderByDescending(m => m.CreatedAt)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a message store for persisting chat messages.
    /// </summary>
    private EfChatMessageStore CreateMessageStore(Guid conversationId)
    {
        var serializedState = JsonSerializer.SerializeToElement(conversationId.ToString("N"));
        return new EfChatMessageStore(
            _chatMessageRepository,
            _unitOfWork,
            serializedState);
    }

    /// <summary>
    /// Creates the tools for the specified conversation mode.
    /// </summary>
    private AIFunction[] CreateTools(ArticleChatTools plugins, ConversationMode mode)
    {
        var tools = new List<AIFunction>
        {
            AIFunctionFactory.Create(
                plugins.SearchFragmentsAsync,
                name: "search_fragments",
                description: "Search for fragments semantically similar to a query string. Returns fragments with similarity scores."
            ),
            AIFunctionFactory.Create(
                plugins.FindSimilarFragmentsAsync,
                name: "find_similar_to_article",
                description: "Find fragments semantically similar to the current article content. Useful for finding related content to enhance or expand the article."
            ),
            AIFunctionFactory.Create(
                plugins.GetFragmentContentAsync,
                name: "get_fragment_content",
                description: "Get the full content and details of a specific fragment by its ID. Use this to review fragments in detail before recommending them."
            )
        };

        if (mode == ConversationMode.Plan)
        {
            tools.Add(AIFunctionFactory.Create(
                plugins.CreatePlanAsync,
                name: "create_plan",
                description: "Create a structured improvement plan for the article with fragment recommendations. Each recommendation should include fragmentId, similarityScore, include (bool), reasoning, and instructions."
            ));
        }

        return tools.ToArray();
    }

    /// <summary>
    /// Creates a chat agent with the specified system message and tools.
    /// </summary>
    private ChatClientAgent CreateChatAgent(string systemMessage, AIFunction[] tools)
    {
        return new ChatClientAgent(
            _chatClient,
            instructions: systemMessage,
            tools: tools
        );
    }

    /// <summary>
    /// Processes streaming updates from the agent, yielding tool invocations and text deltas.
    /// </summary>
    private async IAsyncEnumerable<ChatStreamUpdate> ProcessStreamingUpdatesAsync(
        IAsyncEnumerable<AgentRunResponseUpdate> agentUpdates,
        Guid conversationId,
        Guid articleId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var accumulatedText = new StringBuilder();
        Guid? lastMessageId = null;
        // Track tool names by callId for results
        var toolCallNames = new Dictionary<string, string>();

        await foreach (var update in agentUpdates.WithCancellation(cancellationToken))
        {
            // Extract tool calls and results from update.Contents
            if (update.Contents != null && update.Contents.Count > 0)
            {
                // Parse message ID if available
                Guid? messageId = null;
                if (!string.IsNullOrEmpty(update.MessageId) && Guid.TryParse(update.MessageId, out var parsedId))
                {
                    messageId = parsedId;
                    lastMessageId = messageId;
                }
                else
                {
                    messageId = lastMessageId;
                }

                // Extract function calls
                var functionCalls = update.Contents.OfType<FunctionCallContent>().ToList();
                foreach (var call in functionCalls)
                {
                    // Store the name for later result matching
                    if (!string.IsNullOrEmpty(call.CallId) && !string.IsNullOrEmpty(call.Name))
                    {
                        toolCallNames[call.CallId] = call.Name;
                    }

                    yield return new ChatStreamUpdate
                    {
                        Type = StreamUpdateType.ToolCall,
                        ToolName = call.Name,
                        ToolCallId = call.CallId,
                        ConversationId = conversationId,
                        ArticleId = articleId,
                        MessageId = messageId
                    };
                }

                // Extract function results
                var functionResults = update.Contents.OfType<FunctionResultContent>().ToList();
                foreach (var result in functionResults)
                {
                    // Get the name from the stored call
                    var toolName = !string.IsNullOrEmpty(result.CallId) && toolCallNames.ContainsKey(result.CallId)
                        ? toolCallNames[result.CallId]
                        : null;

                    yield return new ChatStreamUpdate
                    {
                        Type = StreamUpdateType.ToolResult,
                        ToolName = toolName,
                        ToolCallId = result.CallId,
                        ConversationId = conversationId,
                        ArticleId = articleId,
                        MessageId = messageId
                    };
                }
            }

            // Handle text content
            if (!string.IsNullOrEmpty(update.Text))
            {
                accumulatedText.Append(update.Text);
                
                // Use update.MessageId if available (parse from string), otherwise fall back to last used message ID
                Guid? messageId = null;
                if (!string.IsNullOrEmpty(update.MessageId) && Guid.TryParse(update.MessageId, out var parsedId))
                {
                    messageId = parsedId;
                }
                else
                {
                    messageId = lastMessageId;
                }
                
                if (messageId.HasValue)
                {
                    lastMessageId = messageId;
                }
                
                yield return new ChatStreamUpdate
                {
                    Type = StreamUpdateType.TextDelta,
                    Content = update.Text,
                    ConversationId = conversationId,
                    ArticleId = articleId,
                    MessageId = messageId
                };
            }
        }
    }

    /// <summary>
    /// Creates a completion update for the end of a streaming response.
    /// </summary>
    private ChatStreamUpdate CreateCompletionUpdate(
        DomainChatMessage assistantMessage,
        Guid conversationId,
        Guid articleId)
    {
        return new ChatStreamUpdate
        {
            Type = StreamUpdateType.Complete,
            MessageId = assistantMessage.Id,
            ConversationId = conversationId,
            ArticleId = articleId,
            Content = assistantMessage.Content
        };
    }

    #endregion

}
