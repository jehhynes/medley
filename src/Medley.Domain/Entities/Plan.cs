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
    public virtual Article Article { get; set; } = null!;

    /// <summary>
    /// The conversation that created this plan (optional)
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Navigation property to the conversation
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual ChatConversation? Conversation { get; set; }

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
    public virtual User CreatedBy { get; set; } = null!;

    /// <summary>
    /// When this plan was applied to the article (if applicable)
    /// </summary>
    public DateTimeOffset? AppliedAt { get; set; }

    /// <summary>
    /// Fragment recommendations for this plan
    /// </summary>
    public virtual ICollection<PlanFragment> PlanFragments { get; set; } = new List<PlanFragment>();
}
