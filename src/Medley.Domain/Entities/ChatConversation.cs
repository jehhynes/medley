using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a chat conversation between a user and the AI assistant about an article
/// </summary>
[Index(nameof(ArticleId), nameof(State), Name = "IX_ChatConversations_ArticleId_State")]
[Index(nameof(CreatedAt), Name = "IX_ChatConversations_CreatedAt")]
public class ChatConversation : BaseEntity
{
    /// <summary>
    /// The article this conversation is about
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the article
    /// </summary>
    [ForeignKey(nameof(ArticleId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Article Article { get; set; } = null!;

    /// <summary>
    /// Current state of the conversation
    /// </summary>
    public ConversationState State { get; set; } = ConversationState.Active;

    /// <summary>
    /// When this conversation was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this conversation was completed (if applicable)
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The user who created this conversation
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who created the conversation
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual User CreatedBy { get; set; } = null!;

    /// <summary>
    /// Messages in this conversation
    /// </summary>
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
