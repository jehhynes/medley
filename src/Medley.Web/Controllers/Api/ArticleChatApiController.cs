using Hangfire;
using Medley.Application.Helpers;
using Medley.Application.Hubs;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Medley.Web.Controllers.Api;

[Authorize]
[Route("api/articles/{articleId}/assistant")]
[ApiController]
public class ArticleChatApiController : ControllerBase
{
    private readonly IArticleChatService _chatService;
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Domain.Entities.ChatMessage> _chatMessageRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHubContext<ArticleHub> _hubContext;
    private readonly ILogger<ArticleChatApiController> _logger;
    private readonly ToolDisplayExtractor _toolDisplayExtractor;
    private readonly IMedleyContext _medleyContext;

    public ArticleChatApiController(
        IArticleChatService chatService,
        IRepository<ChatConversation> conversationRepository,
        IRepository<Article> articleRepository,
        IRepository<Domain.Entities.ChatMessage> chatMessageRepository,
        IRepository<Plan> planRepository,
        IRepository<User> userRepository,
        IBackgroundJobClient backgroundJobClient,
        IHubContext<ArticleHub> hubContext,
        ILogger<ArticleChatApiController> logger,
        ToolDisplayExtractor toolDisplayExtractor,
        IMedleyContext medleyContext)
    {
        _chatService = chatService;
        _conversationRepository = conversationRepository;
        _articleRepository = articleRepository;
        _chatMessageRepository = chatMessageRepository;
        _planRepository = planRepository;
        _userRepository = userRepository;
        _backgroundJobClient = backgroundJobClient;
        _hubContext = hubContext;
        _logger = logger;
        _toolDisplayExtractor = toolDisplayExtractor;
        _medleyContext = medleyContext;
    }

    /// <summary>
    /// Get a conversation for an article (active or by ID)
    /// </summary>
    [HttpGet("conversation/{conversationId?}")]
    public async Task<IActionResult> GetConversation(Guid articleId, Guid? conversationId = null)
    {
        var article = await _articleRepository.Query()
            .Include(a => a.CurrentConversation)
            .FirstOrDefaultAsync(a => a.Id == articleId);
        
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        ChatConversation? conversation;

        if (conversationId.HasValue)
        {
            // Get specific conversation by ID
            conversation = await _chatService.GetConversationAsync(conversationId.Value);
            
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            // Verify conversation belongs to this article
            if (conversation.ArticleId != articleId)
            {
                return NotFound(new { error = "Conversation not found" });
            }
        }
        else
        {
            // Get current active conversation from article
            conversation = article.CurrentConversation;
            
            if (conversation == null)
            {
                return NoContent(); // No active conversation is a valid state, not an error
            }
        }

        // Load plan info if implementing a plan
        int? planVersion = null;
        if (conversation.ImplementingPlanId.HasValue)
        {
            var plan = await _planRepository.GetByIdAsync(conversation.ImplementingPlanId.Value);
            planVersion = plan?.Version;
        }

        return Ok(new
        {
            id = conversation.Id,
            state = conversation.State.ToString(),
            mode = conversation.Mode.ToString(),
            isRunning = conversation.IsRunning,
            createdAt = conversation.CreatedAt,
            createdBy = conversation.CreatedByUserId,
            implementingPlanId = conversation.ImplementingPlanId,
            implementingPlanVersion = planVersion
        });
    }

    /// <summary>
    /// Create a new conversation for an article
    /// </summary>
    [HttpPost("conversation")]
    public async Task<IActionResult> CreateConversation(Guid articleId, [FromQuery] ConversationMode mode = ConversationMode.Agent)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }
  
        // Check if active conversation already exists
        if (article.CurrentConversationId != null)
        {
            return Conflict(new { error = "An active conversation already exists for this article" });
        }
 
        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User ID not found" });
        }
        
        var conversation = await _chatService.CreateConversationAsync(articleId, userId.Value, mode);
 
        return CreatedAtAction(
            nameof(GetConversation),
            new { articleId },
            new
            {
                id = conversation.Id,
                state = conversation.State.ToString(),
                mode = conversation.Mode.ToString(),
                createdAt = conversation.CreatedAt
            });
    }

    /// <summary>
    /// Get messages for a conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(Guid articleId, Guid conversationId, [FromQuery] int? limit = null)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { error = "Conversation not found" });
        }

        var messages = await _chatService.GetConversationMessagesAsync(conversationId, limit);

        // First: deserialize all messages
        var deserializedMessages = messages.Select(m => new
        {
            Message = m,
            AiMessage = !string.IsNullOrEmpty(m.SerializedMessage)
                ? TryDeserializeMessage(m.SerializedMessage, m.Id)
                : null
        }).ToList();

        // Second: collect tool call names and completed call IDs with their results and error status
        var toolCallNames = new Dictionary<string, string>(); // Map callId -> toolName
        var completedCallIds = new HashSet<string>();
        var completedCallResults = new Dictionary<string, List<Guid>>();
        var erroredCallIds = new HashSet<string>(); // Track which calls resulted in errors
        
        // First pass: collect all tool call names by call ID from all messages
        foreach (var item in deserializedMessages.Where(x => x.AiMessage?.Contents != null))
        {
            foreach (var callContent in item.AiMessage!.Contents.OfType<FunctionCallContent>())
            {
                if (!string.IsNullOrEmpty(callContent.CallId) && !string.IsNullOrEmpty(callContent.Name))
                {
                    toolCallNames[callContent.CallId] = callContent.Name;
                }
            }
        }
        
        // Second pass: process results with tool names from all messages
        foreach (var item in deserializedMessages.Where(x => x.AiMessage?.Contents != null))
        {
            foreach (var resultContent in item.AiMessage!.Contents.OfType<FunctionResultContent>())
            {
                if (!string.IsNullOrEmpty(resultContent.CallId))
                {
                    completedCallIds.Add(resultContent.CallId);
                    
                    // Check if this result is an error
                    if (_toolDisplayExtractor.IsErrorResult(resultContent))
                    {
                        erroredCallIds.Add(resultContent.CallId);
                    }
                    
                    // Get the tool name for this call ID
                    var toolName = toolCallNames.ContainsKey(resultContent.CallId) 
                        ? toolCallNames[resultContent.CallId] 
                        : null;
                    
                    // Extract result IDs using the shared helper with tool name
                    var resultIds = _toolDisplayExtractor.ExtractResultIds(toolName, resultContent);
                    if (resultIds != null && resultIds.Count > 0)
                    {
                        completedCallResults[resultContent.CallId] = resultIds;
                    }
                }
            }
        }

        // Third: build final result with completion status (exclude Tool messages)
        var results = new List<object>();
        foreach (var item in deserializedMessages.Where(item => item.Message.Role != ChatMessageRole.Tool))
        {
            List<object>? toolCalls = null;
            
            if (item.AiMessage?.Contents != null)
            {
                var calls = new List<object>();
                foreach (var fc in item.AiMessage.Contents.OfType<FunctionCallContent>())
                {
                    // Get result IDs if this call was completed
                    List<Guid>? resultIds = !string.IsNullOrEmpty(fc.CallId) && completedCallResults.ContainsKey(fc.CallId)
                        ? completedCallResults[fc.CallId]
                        : null;
                    
                    // Check if this call resulted in an error
                    var isError = !string.IsNullOrEmpty(fc.CallId) && erroredCallIds.Contains(fc.CallId);
                    
                    calls.Add(new {
                        name = fc.Name,
                        callId = fc.CallId,
                        display = await _toolDisplayExtractor.ExtractToolDisplayAsync(fc.Name, fc.Arguments),
                        completed = !string.IsNullOrEmpty(fc.CallId) && completedCallIds.Contains(fc.CallId),
                        isError = isError,
                        result = resultIds != null ? new { ids = resultIds.Select(id => id.ToString()).ToArray() } : null
                    });
                }
                
                if (calls.Any())
                {
                    toolCalls = calls;
                }
            }

            results.Add(new
            {
                id = item.Message.Id,
                role = item.Message.Role.ToString().ToLower(),
                text = item.Message.Text,
                userName = item.Message.User?.FullName,
                createdAt = item.Message.CreatedAt,
                toolCalls = toolCalls ?? new List<object>()
            });
        }

        return Ok(results);
    }

    /// <summary>
    /// Send a message in a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid articleId,
        Guid conversationId,
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message cannot be empty" });
        }

        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { error = "Conversation not found" });
        }

        if (conversation.State != ConversationState.Active)
        {
            return BadRequest(new { error = "Conversation is not active" });
        }

        // Update mode if provided
        if (request.Mode.HasValue)
        {
            conversation.Mode = request.Mode.Value;
        }

        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User ID not found" });
        }

        // Save user message
        var userMessage = new Domain.Entities.ChatMessage
        {
            ConversationId = conversationId,
            UserId = userId.Value,
            Role = ChatMessageRole.User,
            Text = request.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Update IsRunning status immediately for UI responsiveness
        conversation.IsRunning = true;
        
        // Update article's current conversation reference and auto-assign if in Plan mode
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article != null)
        {
            article.CurrentConversationId = conversationId;
            
            // Auto-assign to current user if in Plan mode
            if (conversation.Mode == ConversationMode.Plan && article.AssignedUserId != userId.Value)
            {
                article.AssignedUserId = userId.Value;
                
                // Load user data for SignalR notification
                var assignedUser = await _userRepository.GetByIdAsync(userId.Value);
                
                // Register assignment notification
                var assignmentNotification = new
                {
                    ArticleId = articleId.ToString(),
                    UserId = assignedUser?.Id.ToString(),
                    UserName = assignedUser?.FullName,
                    UserInitials = assignedUser?.Initials,
                    UserColor = assignedUser?.Color,
                    Timestamp = DateTimeOffset.UtcNow
                };
                
                HttpContext.RegisterPostCommitAction(async () =>
                {
                    await _hubContext.Clients.All.SendAsync("ArticleAssignmentChanged", assignmentNotification);
                });
            }
            
            await _articleRepository.SaveAsync(article);
        }
        
        await _conversationRepository.SaveAsync(conversation);
        await _chatMessageRepository.SaveAsync(userMessage);

        // Get user's full name for broadcast
        var user = await _userRepository.GetByIdAsync(userId.Value);
        var userName = user?.FullName ?? "User";


        // Register SignalR notification to be sent after transaction commits
        RegisterSignalRNotification(
            $"Article_{articleId}",
            "ChatMessageReceived",
            new
            {
                id = userMessage.Id.ToString(),
                conversationId = conversationId.ToString(),
                role = "user",
                text = request.Message,
                userName = userName,
                createdAt = userMessage.CreatedAt,
                articleId = articleId.ToString()
            });


        // Capture variables for post-commit actions
        var messageId = userMessage.Id;
        var capturedConversationId = conversationId;

        // Register background job to be enqueued after transaction commits
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                var jobId = _backgroundJobClient.Enqueue<ArticleChatJob>(
                    job => job.ProcessChatMessageAsync(
                        messageId,
                        null!,
                        CancellationToken.None));

                _logger.LogInformation("Enqueued chat job {JobId} for conversation {ConversationId}", jobId, capturedConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing chat job for message {MessageId}", messageId);
            }
        });

        return Ok(new
        {
            messageId = userMessage.Id,
            conversationId = conversationId
        });
    }

    /// <summary>
    /// Get conversation history for an article
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        var conversations = await _chatService.GetConversationHistoryAsync(articleId);

        return Ok(conversations.Select(c => new
        {
            id = c.Id,
            state = c.State.ToString(),
            createdAt = c.CreatedAt,
            messageCount = c.Messages.Count,
            completedAt = c.CompletedAt
        }));
    }

    /// <summary>
    /// Mark a conversation as complete
    /// </summary>
    [HttpPost("conversations/{conversationId}/complete")]
    public async Task<IActionResult> CompleteConversation(Guid articleId, Guid conversationId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { error = "Conversation not found" });
        }

        await _chatService.CompleteConversationAsync(conversationId);

        var completedAt = DateTimeOffset.UtcNow;

        // Register SignalR notification to be sent after transaction commits
        RegisterSignalRNotification(
            $"Article_{articleId}",
            "ConversationCompleted",
            new
            {
                conversationId = conversationId.ToString(),
                articleId = articleId.ToString(),
                completedAt = completedAt
            });

        return Ok(new
        {
            id = conversationId,
            state = ConversationState.Complete.ToString(),
            completedAt = completedAt
        });
    }

    /// <summary>
    /// Cancel a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/cancel")]
    public async Task<IActionResult> CancelConversation(Guid articleId, Guid conversationId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { error = "Conversation not found" });
        }

        await _chatService.CancelConversationAsync(conversationId);

        // Register SignalR notification to be sent after transaction commits
        RegisterSignalRNotification(
            $"Article_{articleId}",
            "ConversationCancelled",
            new
            {
                conversationId = conversationId.ToString(),
                articleId = articleId.ToString(),
                timestamp = DateTimeOffset.UtcNow
            });

        return Ok(new
        {
            id = conversationId,
            state = ConversationState.Archived.ToString()
        });
    }

    private Microsoft.Extensions.AI.ChatMessage? TryDeserializeMessage(string serializedMessage, Guid messageId)
    {
        try
        {
            return JsonSerializer.Deserialize<Microsoft.Extensions.AI.ChatMessage>(serializedMessage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize SerializedMessage for message {MessageId}", messageId);
            return null;
        }
    }

    /// <summary>
    /// Register a SignalR notification to be sent after the transaction commits
    /// </summary>
    /// <param name="groupName">The SignalR group name to send to</param>
    /// <param name="methodName">The client method name to invoke</param>
    /// <param name="payload">The payload object to send</param>
    private void RegisterSignalRNotification(string groupName, string methodName, object payload)
    {
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                await _hubContext.Clients.Group(groupName)
                    .SendAsync(methodName, payload);
                
                _logger.LogDebug("SignalR notification sent: {MethodName} to group {GroupName}", methodName, groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification: {MethodName} to group {GroupName}", methodName, groupName);
                // Don't rethrow - SignalR failures shouldn't break the main flow
            }
        });
    }
}

public record SendMessageRequest(string Message, ConversationMode? Mode = null);

