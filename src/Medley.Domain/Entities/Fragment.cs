using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a knowledge fragment extracted from organizational data sources
/// </summary>
public class Fragment : BusinessEntity
{
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
