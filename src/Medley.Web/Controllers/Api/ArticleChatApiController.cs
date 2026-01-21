using Hangfire;
using Medley.Application.Helpers;
using Medley.Application.Hubs;
using Medley.Application.Hubs.Clients;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models.DTOs;
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
[ApiExplorerSettings(GroupName = "api")]
public class ArticleChatApiController : ControllerBase
{
    private readonly IArticleChatService _chatService;
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Domain.Entities.ChatMessage> _chatMessageRepository;
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IHubContext<ArticleHub, IArticleClient> _hubContext;
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
        IHubContext<ArticleHub, IArticleClient> hubContext,
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
    [HttpGet("conversation")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationDto>> GetConversation(Guid articleId, [FromQuery] Guid? conversationId = null)
    {
        var article = await _articleRepository.Query()
            .Include(a => a.CurrentConversation)
            .FirstOrDefaultAsync(a => a.Id == articleId);
        
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        ChatConversation? conversation;

        if (conversationId.HasValue)
        {
            // Get specific conversation by ID
            conversation = await _chatService.GetConversationAsync(conversationId.Value);
            
            if (conversation == null)
            {
                return NotFound(new { message = "Conversation not found" });
            }

            // Verify conversation belongs to this article
            if (conversation.ArticleId != articleId)
            {
                return NotFound(new { message = "Conversation not found" });
            }
        }
        else
        {
            // Get current active conversation from article
            conversation = article.CurrentConversation;
            
            if (conversation == null)
            {
                return Ok(null); // No active conversation is a valid state, not an error
            }
        }

        // Load plan info if implementing a plan
        int? planVersion = null;
        if (conversation.ImplementingPlanId.HasValue)
        {
            var plan = await _planRepository.GetByIdAsync(conversation.ImplementingPlanId.Value);
            planVersion = plan?.Version;
        }

        return Ok(new ConversationDto
        {
            Id = conversation.Id,
            State = conversation.State.ToString(),
            Mode = conversation.Mode.ToString(),
            IsRunning = conversation.IsRunning,
            CreatedAt = conversation.CreatedAt,
            CreatedBy = conversation.CreatedByUserId,
            ImplementingPlanId = conversation.ImplementingPlanId,
            ImplementingPlanVersion = planVersion
        });
    }

    /// <summary>
    /// Create a new conversation for an article
    /// </summary>
    [HttpPost("conversation")]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ConversationDto>> CreateConversation(Guid articleId, [FromQuery] ConversationMode mode = ConversationMode.Agent)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }
  
        // Check if active conversation already exists
        if (article.CurrentConversationId != null)
        {
            return Conflict(new { message = "An active conversation already exists for this article" });
        }
 
        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }
        
        var conversation = await _chatService.CreateConversationAsync(articleId, userId.Value, mode);
 
        return CreatedAtAction(
            nameof(GetConversation),
            new { articleId },
            new ConversationDto
            {
                Id = conversation.Id,
                State = conversation.State.ToString(),
                Mode = conversation.Mode.ToString(),
                IsRunning = conversation.IsRunning,
                CreatedAt = conversation.CreatedAt,
                CreatedBy = conversation.CreatedByUserId,
                ImplementingPlanId = conversation.ImplementingPlanId,
                ImplementingPlanVersion = null
            });
    }

    /// <summary>
    /// Get messages for a conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(List<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChatMessageDto>>> GetMessages(Guid articleId, Guid conversationId, [FromQuery] int? limit = null)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { message = "Conversation not found" });
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
        var results = new List<ChatMessageDto>();
        foreach (var item in deserializedMessages.Where(item => item.Message.Role != ChatMessageRole.Tool))
        {
            List<ToolCallDto> toolCalls = new List<ToolCallDto>();
            
            if (item.AiMessage?.Contents != null)
            {
                foreach (var fc in item.AiMessage.Contents.OfType<FunctionCallContent>())
                {
                    // Get result IDs if this call was completed
                    List<Guid>? resultIds = !string.IsNullOrEmpty(fc.CallId) && completedCallResults.ContainsKey(fc.CallId)
                        ? completedCallResults[fc.CallId]
                        : null;
                    
                    // Check if this call resulted in an error
                    var isError = !string.IsNullOrEmpty(fc.CallId) && erroredCallIds.Contains(fc.CallId);
                    var completed = !string.IsNullOrEmpty(fc.CallId) && completedCallIds.Contains(fc.CallId);
                    
                    toolCalls.Add(new ToolCallDto
                    {
                        CallId = fc.CallId ?? string.Empty,
                        Name = fc.Name ?? string.Empty,
                        Display = await _toolDisplayExtractor.ExtractToolDisplayAsync(fc.Name, fc.Arguments),
                        Completed = completed,
                        IsError = isError,
                        Result = resultIds != null ? new ToolCallResultDto { Ids = resultIds.Select(id => id.ToString()).ToList() } : null
                    });
                }
            }

            results.Add(new ChatMessageDto
            {
                Id = item.Message.Id,
                ConversationId = conversationId,
                Role = item.Message.Role,
                Text = item.Message.Text,
                UserName = item.Message.User?.FullName,
                CreatedAt = item.Message.CreatedAt,
                ToolCalls = toolCalls
            });
        }

        return Ok(results);
    }

    /// <summary>
    /// Send a message in a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SendMessageResponse>> SendMessage(
        Guid articleId,
        Guid conversationId,
        [FromBody] SendMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "Message cannot be empty" });
        }

        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        if (conversation.State != ConversationState.Active)
        {
            return BadRequest(new { message = "Conversation is not active" });
        }

        // Update mode if provided
        if (request.Mode.HasValue)
        {
            conversation.Mode = request.Mode.Value;
        }

        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "User ID not found" });
        }

        // Save user message
        var userMessage = new Domain.Entities.ChatMessage
        {
            Conversation = conversation,
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
                HttpContext.RegisterPostCommitAction(async () =>
                {
                    await _hubContext.Clients.All.ArticleAssignmentChanged(new ArticleAssignmentChangedPayload
                    {
                        ArticleId = articleId,
                        UserId = assignedUser?.Id,
                        UserName = assignedUser?.FullName,
                        UserInitials = assignedUser?.Initials,
                        UserColor = assignedUser?.Color,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                });
            }
            
            
        }
        
        await _conversationRepository.AddAsync(conversation);
        await _chatMessageRepository.AddAsync(userMessage);

        // Get user's full name for broadcast
        var user = await _userRepository.GetByIdAsync(userId.Value);
        var userName = user?.FullName ?? "User";


        // Register SignalR notification to be sent after transaction commits
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                await _hubContext.Clients.Group($"Article_{articleId}").ChatMessageReceived(new ChatMessageReceivedPayload
                {
                    MessageId = userMessage.Id,
                    ConversationId = conversationId,
                    Role = ChatMessageRole.User,
                    Text = request.Message,
                    UserName = userName,
                    CreatedAt = userMessage.CreatedAt,
                    ArticleId = articleId
                });
                
                _logger.LogDebug("SignalR notification sent: ChatMessageReceived to group Article_{ArticleId}", articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification: ChatMessageReceived to group Article_{ArticleId}", articleId);
            }
        });


        // Capture variables for post-commit actions
        var messageId = userMessage.Id;
        var capturedConversationId = conversationId;

        // Register background job to be enqueued after transaction commits
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                var jobId = _backgroundJobClient.Enqueue<ArticleChatJob>(job => job.ProcessChatMessageAsync(messageId, default!, default));

                _logger.LogInformation("Enqueued chat job {JobId} for conversation {ConversationId}", jobId, capturedConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing chat job for message {MessageId}", messageId);
            }
        });

        return Ok(new SendMessageResponse
        {
            MessageId = userMessage.Id,
            ConversationId = conversationId
        });
    }

    /// <summary>
    /// Mark a conversation as complete
    /// </summary>
    [HttpPost("conversations/{conversationId}/complete")]
    [ProducesResponseType(typeof(ConversationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationStatusResponse>> CompleteConversation(Guid articleId, Guid conversationId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        await _chatService.CompleteConversationAsync(conversationId);

        var completedAt = DateTimeOffset.UtcNow;

        // Register SignalR notification to be sent after transaction commits
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                await _hubContext.Clients.Group($"Article_{articleId}").ConversationCompleted(new ConversationCompletedPayload
                {
                    ConversationId = conversationId,
                    ArticleId = articleId,
                    CompletedAt = completedAt
                });
                
                _logger.LogDebug("SignalR notification sent: ConversationCompleted to group Article_{ArticleId}", articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification: ConversationCompleted to group Article_{ArticleId}", articleId);
            }
        });

        return Ok(new ConversationStatusResponse
        {
            Id = conversationId,
            State = ConversationState.Complete,
            Timestamp = completedAt
        });
    }

    /// <summary>
    /// Cancel a conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/cancel")]
    [ProducesResponseType(typeof(ConversationStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversationStatusResponse>> CancelConversation(Guid articleId, Guid conversationId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation == null || conversation.ArticleId != articleId)
        {
            return NotFound(new { message = "Conversation not found" });
        }

        await _chatService.CancelConversationAsync(conversationId);

        // Register SignalR notification to be sent after transaction commits
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                await _hubContext.Clients.Group($"Article_{articleId}").ConversationCancelled(new ConversationCancelledPayload
                {
                    ConversationId = conversationId,
                    ArticleId = articleId,
                    Timestamp = DateTimeOffset.UtcNow
                });
                
                _logger.LogDebug("SignalR notification sent: ConversationCancelled to group Article_{ArticleId}", articleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification: ConversationCancelled to group Article_{ArticleId}", articleId);
            }
        });

        return Ok(new ConversationStatusResponse
        {
            Id = conversationId,
            State = ConversationState.Archived,
            Timestamp = DateTimeOffset.UtcNow
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
}

public record SendMessageRequest(string Message, ConversationMode? Mode = null);

