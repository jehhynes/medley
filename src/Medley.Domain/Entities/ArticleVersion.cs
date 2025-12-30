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
[Index(nameof(ArticleId), nameof(VersionNumber), IsUnique = true)]
[Index(nameof(ParentVersionId))]
[Index(nameof(VersionType))]
public class ArticleVersion : BaseEntity
{
    /// <summary>
    /// The article this version belongs to
    /// </summary>
    public Guid ArticleId { get; set; }

    /// <summary>
    /// Navigation property to the article
    /// </summary>
    [ForeignKey(nameof(ArticleId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual Article Article { get; set; } = null!;

    /// <summary>
    /// Full content snapshot at this version
    /// </summary>
    public required string ContentSnapshot { get; set; }

    /// <summary>
    /// Diff patch from previous version (for efficient storage and display)
    /// </summary>
    public string? ContentDiff { get; set; }

    /// <summary>
    /// Sequential version number for this article (1, 2, 3, ...)
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// User ID who created this version
    /// </summary>
    [Column("created_by")]
    public Guid? CreatedById { get; set; }

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
    /// Type of version (User or AI draft)
    /// </summary>
    public VersionType VersionType { get; set; } = VersionType.User;

    /// <summary>
    /// Parent version ID for AI draft chains (nullable for user versions)
    /// </summary>
    public Guid? ParentVersionId { get; set; }

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


