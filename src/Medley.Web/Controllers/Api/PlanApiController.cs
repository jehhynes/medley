using Hangfire;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models.DTOs;
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
[ApiExplorerSettings(GroupName = "api")]
public class PlanApiController : ControllerBase
{
    private readonly IRepository<Plan> _planRepository;
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<PlanKnowledgeUnit> _planKnowledgeUnitRepository;
    private readonly IRepository<ChatConversation> _conversationRepository;
    private readonly IRepository<Domain.Entities.ChatMessage> _messageRepository;
    private readonly IArticleChatService _articleChatService;
    private readonly IMedleyContext _medleyContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<PlanApiController> _logger;

    public PlanApiController(
        IRepository<Plan> planRepository,
        IRepository<Article> articleRepository,
        IRepository<PlanKnowledgeUnit> planKnowledgeUnitRepository,
        IRepository<ChatConversation> conversationRepository,
        IRepository<Domain.Entities.ChatMessage> messageRepository,
        IArticleChatService articleChatService,
        IMedleyContext medleyContext,
        IBackgroundJobClient backgroundJobClient,
        ILogger<PlanApiController> logger)
    {
        _planRepository = planRepository;
        _articleRepository = articleRepository;
        _planKnowledgeUnitRepository = planKnowledgeUnitRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _articleChatService = articleChatService;
        _medleyContext = medleyContext;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all plans for an article (including archived)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlanSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PlanSummaryDto>>> GetAllPlans(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);
        if (article == null)
        {
            return NotFound(new { message = "Article not found" });
        }

        var plans = await _planRepository.Query()
            .Where(p => p.ArticleId == articleId)
            .Include(p => p.CreatedBy)
            .OrderByDescending(p => p.Version)
            .Select(p => new PlanSummaryDto
            {
                Id = p.Id,
                Version = p.Version,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                ChangesSummary = p.ChangesSummary,
                CreatedBy = new UserRef
                {
                    Id = p.CreatedBy.Id,
                    FullName = p.CreatedBy.FullName ?? p.CreatedBy.Email ?? "Unknown"
                }
            })
            .ToListAsync();

        return Ok(plans);
    }

    /// <summary>
    /// Get a specific plan by ID
    /// </summary>
    [HttpGet("{planId}")]
    [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanDto>> GetPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .Include(p => p.PlanKnowledgeUnits)
                .ThenInclude(pku => pku.KnowledgeUnit)
                    .ThenInclude(ku => ku.Category)
            .Include(p => p.CreatedBy)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        return Ok(MapPlanToDto(plan));
    }

    /// <summary>
    /// Restore an archived plan as the active draft
    /// </summary>
    [HttpPost("{planId}/restore")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanActionResponse>> RestorePlan(Guid articleId, Guid planId)
    {
        var planToRestore = await _planRepository.Query()
            .Include(x => x.Article).ThenInclude(x => x.CurrentPlan)
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (planToRestore == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (planToRestore.Status == PlanStatus.Draft)
        {
            return BadRequest(new { message = "Plan is already active" });
        }

        var article = planToRestore.Article;
        

        // Archive current plan (if different)
        if (article.CurrentPlanId.HasValue && article.CurrentPlanId != planId)
        {
            var oldPlan = article.CurrentPlan;
            if (oldPlan != null)
            {
                oldPlan.Status = PlanStatus.Archived;
            }
        }

        // Restore the selected plan and set as current
        planToRestore.Status = PlanStatus.Draft;
        article.CurrentPlan = planToRestore;
        article.CurrentPlanId = planId;

        return Ok(new PlanActionResponse { Success = true, Message = "Plan restored successfully" });
    }

    /// <summary>
    /// Update the instructions for a plan
    /// </summary>
    [HttpPut("{planId}")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanActionResponse>> UpdatePlan(Guid articleId, Guid planId, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { message = "Only draft plans can be updated" });
        }

        plan.Instructions = request.Instructions;

        return Ok(new PlanActionResponse { Success = true, Message = null });
    }

    /// <summary>
    /// Update a plan knowledge unit's include flag
    /// </summary>
    [HttpPut("{planId}/knowledge-units/{knowledgeUnitId}/include")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanActionResponse>> UpdatePlanKnowledgeUnitInclude(
        Guid articleId, 
        Guid planId, 
        Guid knowledgeUnitId, 
        [FromBody] UpdatePlanKnowledgeUnitIncludeRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { message = "Only draft plans can be updated" });
        }

        var planKnowledgeUnit = await _planKnowledgeUnitRepository.Query()
            .Where(pku => pku.PlanId == planId && pku.KnowledgeUnitId == knowledgeUnitId)
            .FirstOrDefaultAsync();

        if (planKnowledgeUnit == null)
        {
            return NotFound(new { message = "Plan knowledge unit not found" });
        }

        planKnowledgeUnit.Include = request.Include;

        return Ok(new PlanActionResponse { Success = true, Message = null });
    }

    /// <summary>
    /// Update a plan knowledge unit's instructions
    /// </summary>
    [HttpPut("{planId}/knowledge-units/{knowledgeUnitId}/instructions")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanActionResponse>> UpdatePlanKnowledgeUnitInstructions(
        Guid articleId, 
        Guid planId, 
        Guid knowledgeUnitId, 
        [FromBody] UpdatePlanKnowledgeUnitInstructionsRequest request)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { message = "Only draft plans can be updated" });
        }

        var planKnowledgeUnit = await _planKnowledgeUnitRepository.Query()
            .Where(pku => pku.PlanId == planId && pku.KnowledgeUnitId == knowledgeUnitId)
            .FirstOrDefaultAsync();

        if (planKnowledgeUnit == null)
        {
            return NotFound(new { message = "Plan knowledge unit not found" });
        }

        planKnowledgeUnit.Instructions = request.Instructions;

        return Ok(new PlanActionResponse { Success = true, Message = null });
    }

    /// <summary>
    /// Accept a plan and begin AI implementation via conversation
    /// </summary>
    [HttpPost("{planId}/accept")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PlanActionResponse>> AcceptPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { message = "Only draft plans can be accepted" });
        }

        // Get current user ID
        var userId = _medleyContext.CurrentUserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        // 1. Archive the planning conversation (if exists)
        var planningConversation = await _conversationRepository.GetByIdAsync(plan.ConversationId);
        if (planningConversation != null)
        {
            planningConversation.State = ConversationState.Archived;
                
        }

        // 2. Create new Agent conversation for implementation
        var agentConversation = await _articleChatService.CreateConversationAsync(
            articleId,
            userId.Value,
            ConversationMode.Agent);

        // 3. Link conversation to plan
        agentConversation.ImplementingPlanId = planId;
        

        // 4. Set plan status to InProgress
        plan.Status = PlanStatus.InProgress;
        

        // 5. Create user message requesting implementation
        var userMessage = new Domain.Entities.ChatMessage
        {
            Conversation = agentConversation,
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
                var jobId = _backgroundJobClient.Enqueue<ArticleChatJob>(job => job.ProcessChatMessageAsync(userMessage.Id, default!, default));

                _logger.LogInformation("Enqueued chat job {JobId} for plan implementation conversation {ConversationId}", 
                    jobId, agentConversation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing chat job for plan {PlanId}", planId);
            }
        });

        return Ok(new PlanActionResponse
        { 
            Success = true, 
            Message = "Plan implementation started",
            ConversationId = agentConversation.Id
        });
    }

    /// <summary>
    /// Reject a plan and archive it
    /// </summary>
    [HttpPost("{planId}/reject")]
    [ProducesResponseType(typeof(PlanActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlanActionResponse>> RejectPlan(Guid articleId, Guid planId)
    {
        var plan = await _planRepository.Query()
            .Where(p => p.Id == planId && p.ArticleId == articleId)
            .FirstOrDefaultAsync();

        if (plan == null)
        {
            return NotFound(new { message = "Plan not found" });
        }

        if (plan.Status != PlanStatus.Draft)
        {
            return BadRequest(new { message = "Only draft plans can be rejected" });
        }

        // Update plan status to Archived
        plan.Status = PlanStatus.Archived;

        return Ok(new PlanActionResponse { Success = true, Message = "Plan rejected and archived" });
    }

    private PlanDto MapPlanToDto(Plan plan)
    {
        return new PlanDto
        {
            Id = plan.Id,
            ArticleId = plan.ArticleId,
            Instructions = plan.Instructions,
            Status = plan.Status,
            Version = plan.Version,
            ChangesSummary = plan.ChangesSummary,
            CreatedAt = plan.CreatedAt,
            CreatedBy = new UserRef
            {
                Id = plan.CreatedBy.Id,
                FullName = plan.CreatedBy.FullName ?? plan.CreatedBy.Email ?? "Unknown"
            },
            KnowledgeUnits = plan.PlanKnowledgeUnits
                .OrderByDescending(x => x.SimilarityScore)
                .ThenBy(x => x.KnowledgeUnit.UpdatedAt)
                .Select(pku => new PlanKnowledgeUnitDto
                {
                    Id = pku.Id,
                    KnowledgeUnitId = pku.KnowledgeUnitId,
                    SimilarityScore = pku.SimilarityScore,
                    Include = pku.Include,
                    Reasoning = pku.Reasoning,
                    Instructions = pku.Instructions,
                    KnowledgeUnit = new KnowledgeUnitInPlanDto
                    {
                        Id = pku.KnowledgeUnit.Id,
                        Title = pku.KnowledgeUnit.Title,
                        Summary = pku.KnowledgeUnit.Summary,
                        Category = pku.KnowledgeUnit.Category.Name,
                        CategoryIcon = pku.KnowledgeUnit.Category.Icon,
                        Content = pku.KnowledgeUnit.Content,
                        Confidence = pku.KnowledgeUnit.Confidence,
                        ConfidenceComment = pku.KnowledgeUnit.ConfidenceComment
                    }
                }).ToList()
        };
    }
}

public record UpdatePlanRequest(string Instructions);

public record UpdatePlanKnowledgeUnitIncludeRequest(bool Include);

public record UpdatePlanKnowledgeUnitInstructionsRequest(string Instructions);
