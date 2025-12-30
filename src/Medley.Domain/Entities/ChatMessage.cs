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
    public virtual ChatConversation Conversation { get; set; } = null!;

    /// <summary>
    /// The user who sent the message (null for assistant messages)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Navigation property to the user
    /// </summary>
    [ForeignKey(nameof(UserId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual User? User { get; set; }

    /// <summary>
    /// Type of the message (User, Assistant, ToolCall)
    /// </summary>
    public ChatMessageType MessageType { get; set; }

    /// <summary>
    /// The content of the message
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// When this message was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional metadata (e.g., tokens used, response time) stored as JSON
    /// </summary>
    public string? Metadata { get; set; }
}

