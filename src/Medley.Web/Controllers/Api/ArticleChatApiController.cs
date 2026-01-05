using Hangfire;
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

    public ArticleChatApiController(
        IArticleChatService chatService,
        IRepository<ChatConversation> conversationRepository,
        IRepository<Article> articleRepository,
        IRepository<Domain.Entities.ChatMessage> chatMessageRepository,
        IRepository<Plan> planRepository,
        IRepository<User> userRepository,
        IBackgroundJobClient backgroundJobClient,
        IHubContext<ArticleHub> hubContext,
        ILogger<ArticleChatApiController> logger)
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
    }

    /// <summary>
    /// Get the active conversation for an article
    /// </summary>
    [HttpGet("conversation")]
    public async Task<IActionResult> GetActiveConversation(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        var conversation = await _chatService.GetActiveConversationAsync(articleId);
        if (conversation == null)
        {
            return NoContent(); // No active conversation is a valid state, not an error
        }

        return Ok(new
        {
            id = conversation.Id,
            state = conversation.State.ToString(),
            mode = conversation.Mode.ToString(),
            isRunning = conversation.IsRunning,
            createdAt = conversation.CreatedAt,
            createdBy = conversation.CreatedByUserId
        });
    }

    /// <summary>
    /// Create a new conversation for an article
    /// </summary>
    [HttpPost("conversation")]
    public async Task<IActionResult> CreateConversation(Guid articleId, [FromQuery] ConversationMode mode = ConversationMode.Chat)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }
 
        // Check if active conversation already exists
        var existingConversation = await _chatService.GetActiveConversationAsync(articleId);
        if (existingConversation != null)
        {
            return Conflict(new { error = "An active conversation already exists for this article" });
        }
 
        var userId = GetUserId();
        var conversation = await _chatService.CreateConversationAsync(articleId, userId, mode);
 
        return CreatedAtAction(
            nameof(GetActiveConversation),
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

        // Second: collect all completed call IDs
        var completedCallIds = new HashSet<string>();
        foreach (var item in deserializedMessages.Where(x => x.AiMessage?.Contents != null))
        {
            foreach (var callId in item.AiMessage!.Contents
                .OfType<FunctionResultContent>()
                .Where(fr => !string.IsNullOrEmpty(fr.CallId))
                .Select(fr => fr.CallId!))
            {
                completedCallIds.Add(callId);
            }
        }

        // Third: build final result with completion status (exclude Tool messages)
        var results = deserializedMessages
            .Where(item => item.Message.Role != ChatMessageRole.Tool) // Filter out Tool messages
            .Select(item =>
            {
                List<object>? toolCalls = null;
                
                if (item.AiMessage?.Contents != null)
                {
                    var calls = item.AiMessage.Contents
                        .OfType<FunctionCallContent>()
                        .Select(fc => new {
                            name = fc.Name,
                            callId = fc.CallId,
                            completed = !string.IsNullOrEmpty(fc.CallId) && completedCallIds.Contains(fc.CallId)
                        } as object)
                        .ToList();
                    
                    if (calls.Any())
                    {
                        toolCalls = calls;
                    }
                }

                return new
                {
                    id = item.Message.Id,
                    role = item.Message.Role.ToString().ToLower(),
                    content = item.Message.Content,
                    userName = item.Message.User?.FullName,
                    createdAt = item.Message.CreatedAt,
                    toolCalls = toolCalls ?? new List<object>()
                };
            }).ToList();

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

        var userId = GetUserId();

        // Save user message
        var userMessage = new Domain.Entities.ChatMessage
        {
            ConversationId = conversationId,
            UserId = userId,
            Role = ChatMessageRole.User,
            Content = request.Message,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Update IsRunning status immediately for UI responsiveness
        conversation.IsRunning = true;
        
        // Update article's current conversation reference
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article != null)
        {
            article.CurrentConversationId = conversationId;
            await _articleRepository.SaveAsync(article);
        }
        
        await _conversationRepository.SaveAsync(conversation);
        await _chatMessageRepository.SaveAsync(userMessage);

        // Get user's full name for broadcast
        var user = await _userRepository.GetByIdAsync(userId);
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
                content = request.Message,
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
            state = ConversationState.Cancelled.ToString()
        });
    }



    /// <summary>
    /// Get the active plan for an article
    /// </summary>
    [HttpGet("plans/active")]
    public async Task<IActionResult> GetActivePlan(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        var plan = await _planRepository.Query()
            .Where(p => p.ArticleId == articleId && p.Status == PlanStatus.Draft)
            .Include(p => p.PlanFragments)
                .ThenInclude(pf => pf.Fragment)
                    .ThenInclude(f => f.Source)
            .Include(p => p.CreatedBy)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NoContent();
        }

        return Ok(new
        {
            id = plan.Id,
            articleId = plan.ArticleId,
            instructions = plan.Instructions,
            status = plan.Status.ToString(),
            createdAt = plan.CreatedAt,
            createdBy = new
            {
                id = plan.CreatedBy.Id,
                name = plan.CreatedBy.FullName ?? plan.CreatedBy.Email
            },
            fragments = plan.PlanFragments.Select(pf => new
            {
                id = pf.Id,
                fragmentId = pf.FragmentId,
                similarityScore = pf.SimilarityScore,
                include = pf.Include,
                reasoning = pf.Reasoning,
                instructions = pf.Instructions,
                fragment = new
                {
                    id = pf.Fragment.Id,
                    title = pf.Fragment.Title,
                    summary = pf.Fragment.Summary,
                    category = pf.Fragment.Category,
                    content = pf.Fragment.Content,
                    confidence = pf.Fragment.Confidence?.ToString(),
                    source = pf.Fragment.Source != null ? new
                    {
                        id = pf.Fragment.Source.Id,
                        name = pf.Fragment.Source.Name,
                        type = pf.Fragment.Source.Type.ToString(),
                        date = pf.Fragment.Source.Date
                    } : null
                }
            }).ToList()
        });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
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

