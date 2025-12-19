using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a version snapshot of article content
/// </summary>
[Index(nameof(ArticleId))]
[Index(nameof(ArticleId), nameof(CreatedAt))]
[Index(nameof(ArticleId), nameof(VersionNumber), IsUnique = true)]
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
}


