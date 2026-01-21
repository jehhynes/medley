using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Medley.Application.Configuration;
using Medley.Application.Helpers;
using Medley.Application.Interfaces;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
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
    private readonly IRepository<AiPrompt> _templateRepository;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatClient _chatClient;
    private readonly ArticleChatToolsFactory _toolsFactory;
    private readonly SystemPromptBuilder _systemPromptBuilder;
    private readonly ILogger<ArticleChatService> _logger;
    private readonly AiCallContext _aiCallContext;
    private readonly ToolDisplayExtractor _toolDisplayExtractor;
    private readonly IConfiguration _configuration;
    
    public ArticleChatService(
        IRepository<ChatConversation> conversationRepository,
        IRepository<DomainChatMessage> chatMessageRepository,
        IRepository<Article> articleRepository,
        IRepository<AiPrompt> templateRepository,
        IRepository<Fragment> fragmentRepository,
        IRepository<Plan> planRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IChatClient chatClient,
        ArticleChatToolsFactory pluginsFactory,
        SystemPromptBuilder systemPromptBuilder,
        ILogger<ArticleChatService> logger,
        AiCallContext aiCallContext,
        ToolDisplayExtractor toolDisplayExtractor,
        IConfiguration configuration)
    {
        _conversationRepository = conversationRepository;
        _chatMessageRepository = chatMessageRepository;
        _articleRepository = articleRepository;
        _templateRepository = templateRepository;
        _fragmentRepository = fragmentRepository;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _toolsFactory = pluginsFactory;
        _systemPromptBuilder = systemPromptBuilder;
        _logger = logger;
        _aiCallContext = aiCallContext;
        _toolDisplayExtractor = toolDisplayExtractor;
        _configuration = configuration;

        // Wrap the provided chat client with function invocation support
        _chatClient = new ChatClientBuilder(chatClient).UseFunctionInvocation().Build();
    }

    public async Task<ChatConversation> CreateConversationAsync(
        Guid articleId,
        Guid userId,
        ConversationMode mode = ConversationMode.Agent,
        CancellationToken cancellationToken = default)
    {
        // Load required navigation properties
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            throw new InvalidOperationException($"Article {articleId} not found");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User {userId} not found");
        }

        var conversation = new ChatConversation
        {
            Article = article,
            State = ConversationState.Active,
            Mode = mode,
            CreatedBy = user,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _conversationRepository.AddAsync(conversation);
        if (article != null)
        {
            article.CurrentConversationId = conversation.Id;
            
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new conversation {ConversationId} for article {ArticleId} by user {UserId}",
            conversation.Id, articleId, userId);

        return conversation;
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
            
            // Get the latest user message to process
            var latestUserMessage = await GetLatestUserMessageAsync(conversationId, cancellationToken);
            
            if (latestUserMessage.User == null)
            {
                throw new InvalidOperationException($"User not found for latest message {latestUserMessage.Id} in conversation {conversationId}");
            }

            // Create article-scoped plugins instance
            var tools = _toolsFactory.Create(article.Id, latestUserMessage.User.Id, conversation.ImplementingPlan?.Id);
            
            // Select system prompt and tools based on mode
            string systemMessage;
            AIFunction[] aiFunctions;
            
            if (conversation.Mode == ConversationMode.Plan)
            {
                // Load the ArticleImprovementPlan template
                var template = await _templateRepository.Query()
                    .FirstOrDefaultAsync(t => t.Type == TemplateType.ArticlePlanCreation, cancellationToken);
                
                if (template == null)
                {
                    throw new InvalidOperationException("Article Improvement Plan template not found");
                }
                
                systemMessage = template.Content.Replace("{article.Title}", article.Title);
                aiFunctions = CreateFunctions(tools, conversation.Mode);
            }
            else if (conversation.Mode == ConversationMode.Agent && conversation.ImplementingPlanId.HasValue)
            {
                // Agent mode implementing a plan - use SystemPromptBuilder
                systemMessage = await _systemPromptBuilder.BuildPromptAsync(
                    conversation.ArticleId,
                    conversation.ImplementingPlanId,
                    TemplateType.ArticlePlanImplementation,
                    cancellationToken);
                
                aiFunctions = CreateFunctions(tools, conversation.Mode);
            }
            else
            {
                // Regular agent chat - load ArticleChat template
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
                
                aiFunctions = CreateFunctions(tools, conversation.Mode);
            }
            
            // Create the message store for persistence
            var messageStore = CreateMessageStore(conversationId);
            
            // Create the agent with appropriate system message and tools
            var agent = CreateChatAgent(systemMessage, aiFunctions);

            // Create or get existing thread with the message store
            var thread = agent.GetNewThread(messageStore);

            // Stream the agent response
            var agentUpdates = agent.RunStreamingAsync(
                Enumerable.Empty<Microsoft.Extensions.AI.ChatMessage>(), //Pass in null as the user message because it's already in the thread.
                thread: thread,
                cancellationToken: cancellationToken);
            
            // Process and yield streaming updates
            await foreach (var update in ProcessStreamingUpdatesAsync(
                agentUpdates,
                conversationId,
                conversation.ArticleId,
                latestUserMessage,
                cancellationToken))
            {
                yield return update;
            }
            
            // Retrieve the most recent assistant message
            var assistantMessage = await GetLatestAssistantMessageAsync(conversationId, cancellationToken);
            
            _logger.LogInformation("Completed streaming for conversation {ConversationId} (Mode: {Mode}), message {MessageId}",
                conversationId, conversation.Mode, assistantMessage.Id);
            
            // Yield turn complete update
            yield return new ChatStreamUpdate
            {
                Type = StreamUpdateType.TurnComplete,
                ConversationId = conversationId,
                ArticleId = conversation.ArticleId,
                MessageId = assistantMessage.Id
            };
        }
    }


    /// <summary>
    /// Processes streaming updates from the agent, yielding tool invocations and text deltas.
    /// </summary>
    private async IAsyncEnumerable<ChatStreamUpdate> ProcessStreamingUpdatesAsync(
        IAsyncEnumerable<AgentRunResponseUpdate> agentUpdates,
        Guid conversationId,
        Guid articleId,
        DomainChatMessage lastMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var accumulatedUpdates = new List<ChatResponseUpdate>();

        // Track tool call info by callId: (toolName, messageId)
        var toolCallInfo = new Dictionary<string, (string toolName, Guid messageId)>();

        await foreach (var update in agentUpdates.WithCancellation(cancellationToken))
        {
            var chatResponseUpdate = update.AsChatResponseUpdate();
            if (chatResponseUpdate.Contents.Count == 1 && chatResponseUpdate.Contents.Single() is UsageContent)
            {
                continue; //Skip usage-only updates
            }
            else
            {
                accumulatedUpdates.Add(chatResponseUpdate);
            }

            // Parse message ID if available
            Guid? messageId = null;
            if (!string.IsNullOrEmpty(update.MessageId) && Guid.TryParse(update.MessageId, out var parsedId))
            {
                messageId = parsedId;
            }
            else
            {
                messageId = lastMessage.Id;
            }

            bool isFunctionResult = false;

            // Extract tool calls and results from update.Contents
            if (update.Contents != null && update.Contents.Count > 0)
            {
                // Extract function calls
                foreach (var call in update.Contents.OfType<FunctionCallContent>())
                {
                    // Store the name and message ID for later result matching
                    if (!string.IsNullOrEmpty(call.CallId) && !string.IsNullOrEmpty(call.Name) && messageId.HasValue)
                    {
                        toolCallInfo[call.CallId] = (call.Name, messageId.Value);
                    }

                    yield return new ChatStreamUpdate
                    {
                        Type = StreamUpdateType.ToolCall,
                        ToolName = call.Name,
                        ToolCallId = call.CallId,
                        ToolDisplay = await _toolDisplayExtractor.ExtractToolDisplayAsync(call.Name, call.Arguments),
                        ConversationId = conversationId,
                        ArticleId = articleId,
                        MessageId = messageId
                    };
                }

                // Extract function results
                foreach (var result in update.Contents.OfType<FunctionResultContent>())
                {
                    isFunctionResult = true;

                    // Get the name and message ID from the stored call info
                    string? toolName = null;
                    Guid? toolCallMessageId = messageId;
                    
                    if (!string.IsNullOrEmpty(result.CallId) && toolCallInfo.ContainsKey(result.CallId))
                    {
                        var info = toolCallInfo[result.CallId];
                        toolName = info.toolName;
                        toolCallMessageId = info.messageId;
                    }

                    // Check if this is an error result
                    var isError = _toolDisplayExtractor.IsErrorResult(result);

                    // Extract result IDs from the tool result
                    var resultIds = _toolDisplayExtractor.ExtractResultIds(toolName, result);

                    yield return new ChatStreamUpdate
                    {
                        Type = StreamUpdateType.ToolResult,
                        ToolName = toolName,
                        ToolCallId = result.CallId,
                        ToolResultIds = resultIds,
                        IsError = isError,
                        ConversationId = conversationId,
                        ArticleId = articleId,
                        MessageId = toolCallMessageId
                    };
                }
            }

            // Handle text content
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new ChatStreamUpdate
                {
                    Type = StreamUpdateType.TextDelta,
                    Text = update.Text,
                    ConversationId = conversationId,
                    ArticleId = articleId,
                    MessageId = messageId
                };
            }

            if (chatResponseUpdate.FinishReason != null || isFunctionResult)
            {
                //This is the end of the message, go ahead and save it.
                var chatResponse = accumulatedUpdates.ToChatResponse();

                if (chatResponseUpdate.FinishReason != null)
                {
                    yield return new ChatStreamUpdate
                    {
                        Type = StreamUpdateType.MessageComplete,
                        MessageId = messageId,
                        ConversationId = conversationId,
                        ArticleId = articleId,
                        Text = chatResponse.Text
                    };
                }

                bool isBalanced = true;
                var functionCalls = chatResponse.Messages.SelectMany(x => x.Contents).OfType<FunctionCallContent>();
                if (functionCalls.Any())
                {
                    var functionResults = chatResponse.Messages.SelectMany(x => x.Contents).OfType<FunctionResultContent>().Select(x => x.CallId).ToHashSet();
                    isBalanced = functionCalls.All(x => functionResults.Contains(x.CallId));
                }

                if (isBalanced) //Wait to save function call messages until we have the result as well. Otherwise it will cause MAF errors due to inconsistent state
                {
                    var messages = await SaveMessages(chatResponse, accumulatedUpdates, conversationId, lastMessage);
                    if (messages.Any())
                        lastMessage = messages.Last();
                    accumulatedUpdates.Clear();
                }
            }
        }
    }

    private async Task<List<DomainChatMessage>> SaveMessages(ChatResponse chatResponse, List<ChatResponseUpdate> responseUpdates, Guid conversationId, DomainChatMessage lastMessage)
    {
        // Load conversation for required navigation property
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        List<DomainChatMessage> result = new();
        foreach (var aiMessage in chatResponse.Messages) //Should only be one message
        {
            // Determine the creation date with proper ordering
            DateTimeOffset createdAt;
            if (aiMessage.CreatedAt.HasValue && aiMessage.CreatedAt.Value > lastMessage.CreatedAt)
            {
                createdAt = aiMessage.CreatedAt.Value;
            }
            else
            {
                // Use last date + 1 ms to maintain order
                createdAt = lastMessage.CreatedAt.AddMilliseconds(1);
            }

            var role = EfChatMessageStore.MapChatRoleToMessageRole(aiMessage.Role);

            var message = new DomainChatMessage()
            {
                Id = role == ChatMessageRole.Tool || aiMessage.MessageId == null ? Guid.NewGuid() : Guid.Parse(aiMessage.MessageId), //Tool messages don't have unique IDs from the chat client
                Conversation = conversation,
                Role = role,
                Text = aiMessage.Text ?? string.Empty,
                CreatedAt = aiMessage.CreatedAt ?? DateTimeOffset.UtcNow,
                UserId = null, // Assistant message has no user id
                SerializedMessage = JsonSerializer.Serialize(aiMessage)
            };

            result.Add(message);

            await _chatMessageRepository.AddAsync(message);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            await _unitOfWork.BeginTransactionAsync();
        }

        return result;
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

        
        
        // Clear the article's current conversation reference if it matches this conversation
        var article = await _articleRepository.GetByIdAsync(conversation.ArticleId);
        if (article != null && article.CurrentConversationId == conversationId)
        {
            article.CurrentConversationId = null;
            
        }
        
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

        conversation.State = ConversationState.Archived;

        
        
        // Clear the article's current conversation reference if it matches this conversation
        var article = await _articleRepository.GetByIdAsync(conversation.ArticleId);
        if (article != null && article.CurrentConversationId == conversationId)
        {
            article.CurrentConversationId = null;
            
        }
        
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
            await _conversationRepository.AddAsync(conversation);
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
            _conversationRepository,
            _unitOfWork,
            serializedState);
    }

    /// <summary>
    /// Creates the tools for the specified conversation mode.
    /// </summary>
    private AIFunction[] CreateFunctions(ArticleChatTools plugins, ConversationMode mode)
    {
        var tools = new List<AIFunction>
        {
            AIFunctionFactory.Create(plugins.SearchFragmentsAsync),
            AIFunctionFactory.Create(plugins.FindSimilarFragmentsAsync),
            AIFunctionFactory.Create(plugins.GetFragmentContentAsync)
        };

        if (mode == ConversationMode.Plan)
        {
            tools.Add(AIFunctionFactory.Create(plugins.CreatePlanAsync));
        }

        if (mode == ConversationMode.Agent)
        {
            tools.Add(AIFunctionFactory.Create(plugins.CreateArticleVersionAsync));
        }

        // Add Cursor tools if enabled
        var cursorSettings = _configuration.GetSection("Cursor").Get<CursorSettings>();
        if (cursorSettings?.Enabled == true)
        {
            tools.Add(AIFunctionFactory.Create(plugins.ReviewArticleWithCursorAsync));
            tools.Add(AIFunctionFactory.Create(plugins.AskQuestionWithCursorAsync));
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

    #endregion

}
