using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a version snapshot of article content
/// </summary>
[Index(nameof(ArticleId))]
[Index(nameof(ArticleId), nameof(CreatedAt))]
[Index(nameof(ArticleId), nameof(VersionNumber), nameof(ParentVersionId), nameof(VersionType), IsUnique = true)]
[Index(nameof(ParentVersionId))]
[Index(nameof(VersionType))]
[Index(nameof(ConversationId))]
public class ArticleVersion : BaseEntity
{
    /// <summary>
    /// The article this version belongs to
    /// </summary>
    protected Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the article
    /// </summary>
    [ForeignKey(nameof(ArticleId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required virtual Article Article { get; set; }

    /// <summary>
    /// The conversation that created this version (optional)
    /// </summary>
    protected Guid? ConversationId { get; set; }

    /// <summary>
    /// Navigation property to the conversation
    /// </summary>
    [ForeignKey(nameof(ConversationId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual ChatConversation? Conversation { get; set; }

    /// <summary>
    /// Full content snapshot at this version
    /// </summary>
    public required string ContentSnapshot { get; set; }

    /// <summary>
    /// Diff patch from previous version (for efficient storage and display)
    /// </summary>
    public string? ContentDiff { get; set; }

    /// <summary>
    /// Version number for this version.
    /// For User versions: Global sequence (1, 2, 3, ...) scoped to the article.
    /// For AI versions: Sequence scoped to the parent User version (1, 2, 3, ... per parent).
    /// </summary>
    public required int VersionNumber { get; set; }

    /// <summary>
    /// User ID who created this version
    /// </summary>
    [Column("created_by")]
    protected Guid? CreatedById { get; set; }

    /// <summary>
    /// Navigation property to the user who created this version
    /// </summary>
    [ForeignKey(nameof(CreatedById))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual User? CreatedBy { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this version was last modified (for draft versions that get updated)
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Type of version (User or AI draft)
    /// </summary>
    public VersionType VersionType { get; set; } = VersionType.User;

    /// <summary>
    /// Parent version ID. 
    /// For User versions: Always null (User versions have no parent).
    /// For AI versions: Always references the most recent User version at creation time.
    /// </summary>
    protected Guid? ParentVersionId { get; set; }

    /// <summary>
    /// Navigation property to parent version
    /// </summary>
    [ForeignKey(nameof(ParentVersionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual ArticleVersion? ParentVersion { get; set; }

    /// <summary>
    /// Commit-style message describing changes in this version
    /// </summary>
    [MaxLength(500)]
    public string? ChangeMessage { get; set; }

    /// <summary>
    /// Indicates if this version is currently active in its chain
    /// </summary>
    public bool IsActive { get; set; } = true;
}


