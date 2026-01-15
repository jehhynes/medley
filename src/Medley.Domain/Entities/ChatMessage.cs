using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a message in a chat conversation
/// </summary>
[Index(nameof(ConversationId), nameof(CreatedAt), Name = "IX_ChatMessages_ConversationId_CreatedAt")]
public class ChatMessage : BaseEntity
{
    /// <summary>
    /// The conversation this message belongs to
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Navigation property to the conversation
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required virtual ChatConversation Conversation { get; set; }

    /// <summary>
    /// The user who sent the message (null for assistant/system/tool messages)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual User? User { get; set; }

    /// <summary>
    /// Role of the message (User, Assistant, System, Tool)
    /// </summary>
    public required ChatMessageRole Role { get; set; }

    /// <summary>
    /// The content of the message
    /// </summary>
    public required string? Text { get; set; }

    /// <summary>
    /// When this message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Full serialized Microsoft.Extensions.AI ChatMessage as JSON for Agent Framework
    /// </summary>
    public string? SerializedMessage { get; set; }
}
