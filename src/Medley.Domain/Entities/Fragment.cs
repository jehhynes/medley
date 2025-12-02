using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a knowledge fragment extracted from organizational data sources
/// </summary>
public class Fragment : BusinessEntity
{
    /// <summary>
    /// Title of the fragment
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Brief summary of the fragment
    /// </summary>
    [MaxLength(500)]
    public string? Summary { get; set; }

    /// <summary>
    /// Category of the fragment (e.g., Decision, Action Item, Feature Request)
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// The text content of the fragment
    /// </summary>
    [MaxLength(10000)]
    public required string Content { get; set; }

    /// <summary>
    /// Vector embedding for semantic similarity operations (1536 dimensions for Claude 4.5)
    /// Stored as float[] in domain, mapped to vector(1536) in database
    /// </summary>
    [DataType("vector(1536)")]
    public float[]? Embedding { get; set; }

    /// <summary>
    /// Navigation to the originating Source
    /// </summary>
    public required virtual Source Source { get; set; }

    /// <summary>
    /// Date when the fragment was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}
