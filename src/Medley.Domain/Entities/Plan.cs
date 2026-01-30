using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents an AI-generated improvement plan for an article
/// </summary>
[Index(nameof(ArticleId), nameof(Status))]
[Index(nameof(ConversationId))]
public class Plan : BaseEntity
{
    /// <summary>
    /// The article this plan is for
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the article
    /// </summary>
    [ForeignKey(nameof(ArticleId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required virtual Article Article { get; set; }

    /// <summary>
    /// The conversation that created this plan (optional)
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Navigation property to the conversation
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public required virtual ChatConversation Conversation { get; set; }

    /// <summary>
    /// Markdown-formatted instructions from the AI agent
    /// </summary>
    public required string Instructions { get; set; }

    /// <summary>
    /// Current status of the plan
    /// </summary>
    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    /// <summary>
    /// When this plan was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created this plan
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who created the plan
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required virtual User CreatedBy { get; set; }

    /// <summary>
    /// When this plan was applied to the article (if applicable)
    /// </summary>
    public DateTimeOffset? AppliedAt { get; set; }

    /// <summary>
    /// Version number of this plan (per article)
    /// </summary>
    public required int Version { get; set; } = 1;

    /// <summary>
    /// Reference to the parent plan this was derived from (if modified)
    /// </summary>
    public Guid? ParentPlanId { get; set; }

    /// <summary>
    /// Navigation property to the parent plan
    /// </summary>
    [ForeignKey(nameof(ParentPlanId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Plan? ParentPlan { get; set; }

    /// <summary>
    /// AI-generated summary of changes from parent plan
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// Child plans derived from this plan
    /// </summary>
    public virtual ICollection<Plan> ChildPlans { get; set; } = new List<Plan>();

    /// <summary>
    /// Knowledge unit recommendations for this plan
    /// </summary>
    public virtual ICollection<PlanKnowledgeUnit> PlanKnowledgeUnits { get; set; } = new List<PlanKnowledgeUnit>();
}
