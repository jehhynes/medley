using Hangfire;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Medley.Web.Controllers.Api;

[Authorize]
[Route("api/articles/{articleId}/plans")]
[ApiController]
public class PlanApiController : ControllerBase
{
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<PlanFragment> _planFragmentRepository;
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Domain.Entities.ChatMessage> _messageRepository;
    private readonly IArticleChatService _articleChatService;
    private readonly SystemPromptBuilder _systemPromptBuilder;
    private readonly IMedleyContext _medleyContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<PlanApiController> _logger;

    public PlanApiController(
        IRepository<Plan> planRepository,
        IRepository<Article> articleRepository,
        IRepository<PlanFragment> planFragmentRepository,
        IRepository<ChatConversation> conversationRepository,
        IRepository<Domain.Entities.ChatMessage> messageRepository,
        IArticleChatService articleChatService,
        SystemPromptBuilder systemPromptBuilder,
        IMedleyContext medleyContext,
        IBackgroundJobClient backgroundJobClient,
        ILogger<PlanApiController> logger)
    {
        _planRepository = planRepository;
        _articleRepository = articleRepository;
        _planFragmentRepository = planFragmentRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _articleChatService = articleChatService;
        _systemPromptBuilder = systemPromptBuilder;
        _medleyContext = medleyContext;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Get the active plan for an article
    /// </summary>
    [HttpGet("active")]
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

        return Ok(MapPlanToDto(plan));
    }

    /// <summary>
    /// Get all plans for an article (including archived)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllPlans(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { error = "Article not found" });
        }

        var plans = await _planRepository.Query()
            .Where(p => p.ArticleId == articleId)
            .Include(p => p.CreatedBy)
            .OrderByDescending(p => p.Version)
            .Select(p => new
            {
                id = p.Id,
                version = p.Version,
                status = p.Status.ToString(),
                createdAt = p.CreatedAt,
                changesSummary = p.ChangesSummary,
                createdBy = new
                {
                    id = p.CreatedBy.Id,
                    name = p.CreatedBy.FullName ?? p.CreatedBy.Email
                }
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Get a specific plan by ID
    /// </summary>
    [HttpGet("{planId}")]
    public async Task<IActionResult> GetPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .Include(p => p.PlanFragments)
                .ThenInclude(pf => pf.Fragment)
                    .ThenInclude(f => f.Source)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        return Ok(MapPlanToDto(plan));
    }

    /// <summary>
    /// Restore an archived plan as the active draft
    /// </summary>
    [HttpPost("{planId}/restore")]
    public async Task<IActionResult> RestorePlan(Guid articleId, Guid planId)
    {
        var planToRestore = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (planToRestore == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (planToRestore.Status == PlanStatus.Draft)
        {
            return BadRequest(new { error = "Plan is already active" });
        }

        // Archive current draft plan (if any)
        var currentDraft = await _planRepository.Query()
            .Where(p => p.ArticleId == articleId && p.Status == PlanStatus.Draft)
            .FirstOrDefaultAsync();

        if (currentDraft != null)
        {
            currentDraft.Status = PlanStatus.Archived;
        }

        // Restore the selected plan
        planToRestore.Status = PlanStatus.Draft;

        return Ok(new { success = true, message = "Plan restored successfully" });
    }

    /// <summary>
    /// Update the instructions for a plan
    /// </summary>
    [HttpPut("{planId}")]
    public async Task<IActionResult> UpdatePlan(Guid articleId, Guid planId, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { error = "Only draft plans can be updated" });
        }

        plan.Instructions = request.Instructions;

        return Ok(new { success = true });
    }

    /// <summary>
    /// Update a plan fragment's include flag
    /// </summary>
    [HttpPut("{planId}/fragments/{fragmentId}/include")]
    public async Task<IActionResult> UpdatePlanFragmentInclude(
        Guid articleId, 
        Guid planId, 
        Guid fragmentId, 
        [FromBody] UpdatePlanFragmentIncludeRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { error = "Only draft plans can be updated" });
        }

        var planFragment = await _planFragmentRepository.Query()
            .Where(pf => pf.PlanId == planId && pf.FragmentId == fragmentId)
            .FirstOrDefaultAsync();

        if (planFragment == null)
        {
            return NotFound(new { error = "Plan fragment not found" });
        }

        planFragment.Include = request.Include;

        return Ok(new { success = true });
    }

    /// <summary>
    /// Update a plan fragment's instructions
    /// </summary>
    [HttpPut("{planId}/fragments/{fragmentId}/instructions")]
    public async Task<IActionResult> UpdatePlanFragmentInstructions(
        Guid articleId, 
        Guid planId, 
        Guid fragmentId, 
        [FromBody] UpdatePlanFragmentInstructionsRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { error = "Only draft plans can be updated" });
        }

        var planFragment = await _planFragmentRepository.Query()
            .Where(pf => pf.PlanId == planId && pf.FragmentId == fragmentId)
            .FirstOrDefaultAsync();

        if (planFragment == null)
        {
            return NotFound(new { error = "Plan fragment not found" });
        }

        planFragment.Instructions = request.Instructions;

        return Ok(new { success = true });
    }

    /// <summary>
    /// Accept a plan and begin AI implementation via conversation
    /// </summary>
    [HttpPost("{planId}/accept")]
    public async Task<IActionResult> AcceptPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { error = "Only draft plans can be accepted" });
        }

        // Get current user ID
        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        // 1. Archive the planning conversation (if exists)
        if (plan.ConversationId.HasValue)
        {
            var planningConversation = await _conversationRepository.GetByIdAsync(plan.ConversationId.Value);
            if (planningConversation != null)
            {
                planningConversation.State = ConversationState.Archived;
                // Entity is already tracked, changes will be saved on SaveChangesAsync
            }
        }

        // 2. Create new Agent conversation for implementation
        var agentConversation = await _articleChatService.CreateConversationAsync(
            articleId,
            userId.Value,
            ConversationMode.Agent);

        // 3. Link conversation to plan
        agentConversation.ImplementingPlanId = planId;
        // Entity is already tracked, changes will be saved on SaveChangesAsync

        // 4. Set plan status to InProgress
        plan.Status = PlanStatus.InProgress;
        // Entity is already tracked, changes will be saved on SaveChangesAsync

        // 5. Create user message requesting implementation
        var userMessage = new Domain.Entities.ChatMessage
        {
            ConversationId = agentConversation.Id,
            UserId = userId.Value,
            Role = ChatMessageRole.User,
            Text = "Please implement the improvement plan as described.",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _messageRepository.AddAsync(userMessage);

        // 6. Register post-commit actions
        HttpContext.RegisterPostCommitAction(async () =>
        {
            try
            {
                // Enqueue existing ArticleChatJob (reuse existing pipeline)
                var jobId = _backgroundJobClient.Enqueue<ArticleChatJob>(
                    job => job.ProcessChatMessageAsync(
                        userMessage.Id,
                        null!,
                        CancellationToken.None));

                _logger.LogInformation("Enqueued chat job {JobId} for plan implementation conversation {ConversationId}", 
                    jobId, agentConversation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing chat job for plan {PlanId}", planId);
            }
        });

        return Ok(new 
        { 
            success = true, 
            conversationId = agentConversation.Id,
            message = "Plan implementation started"
        });
    }

    /// <summary>
    /// Reject a plan and archive it
    /// </summary>
    [HttpPost("{planId}/reject")]
    public async Task<IActionResult> RejectPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { error = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { error = "Only draft plans can be rejected" });
        }

        // Update plan status to Archived
        plan.Status = PlanStatus.Archived;

        return Ok(new { success = true, message = "Plan rejected and archived" });
    }

    private object MapPlanToDto(Plan plan)
    {
        return new
        {
            id = plan.Id,
            articleId = plan.ArticleId,
            instructions = plan.Instructions,
            status = plan.Status.ToString(),
            version = plan.Version,
            changesSummary = plan.ChangesSummary,
            createdAt = plan.CreatedAt,
            createdBy = new
            {
                id = plan.CreatedBy.Id,
                name = plan.CreatedBy.FullName ?? plan.CreatedBy.Email
            },
            fragments = plan.PlanFragments
                .OrderByDescending(x => x.SimilarityScore)
                .ThenBy(x => x.Fragment.Source?.CreatedAt)
                .ThenBy(x => x.Fragment.CreatedAt).Select(pf => new
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
                        confidenceComment = pf.Fragment.ConfidenceComment,
                        source = pf.Fragment.Source != null ? new
                        {
                            id = pf.Fragment.Source.Id,
                            name = pf.Fragment.Source.Name,
                            type = pf.Fragment.Source.Type.ToString(),
                            date = pf.Fragment.Source.Date
                        } : null
                    }
                }).ToList()
        };
    }
}

public record UpdatePlanRequest(string Instructions);

public record UpdatePlanFragmentIncludeRequest(bool Include);

public record UpdatePlanFragmentInstructionsRequest(string Instructions);
