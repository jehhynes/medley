using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a knowledge fragment extracted from organizational data sources
/// </summary>
public class Fragment
{
    /// <summary>
    /// Unique identifier for the fragment
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The text content of the fragment
    /// </summary>
    [Required]
    [MaxLength(10000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding for semantic similarity operations (1536 dimensions for Claude 4.5)
    /// Stored as float[] in domain, mapped to vector(1536) in database
    /// </summary>
    [DataType("vector(1536)")]
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Source type (e.g., "Fellow.ai", "GitHub")
    /// </summary>
    [MaxLength(100)]
    public string? SourceType { get; set; }

    /// <summary>
    /// Source identifier (e.g., meeting ID, PR number)
    /// </summary>
    [MaxLength(500)]
    public string? SourceId { get; set; }

    /// <summary>
    /// Date when the fragment was created
    /// </summary>
    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Date when the fragment was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}
