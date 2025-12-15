using Medley.Domain.Enums;
using Pgvector;
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
    /// Vector embedding for semantic similarity operations
    /// </summary>
    [Column(TypeName = "vector(2000)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Navigation to the originating Source
    /// </summary>
    public virtual Source? Source { get; set; }

    /// <summary>
    /// Date when the fragment was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Indicates if this fragment is a cluster of other fragments
    /// </summary>
    public bool IsCluster { get; set; }

    /// <summary>
    /// The ID of the cluster fragment this fragment belongs to
    /// </summary>
    public Guid? ClusteredIntoId { get; set; }

    /// <summary>
    /// Navigation to the cluster fragment
    /// </summary>
    [ForeignKey(nameof(ClusteredIntoId))]
    public virtual Fragment? ClusteredInto { get; set; }

    /// <summary>
    /// Date when the fragment was processed for clustering
    /// </summary>
    public DateTimeOffset? ClusteringProcessed { get; set; }

    /// <summary>
    /// Confidence level from the AI extraction
    /// </summary>
    public ConfidenceLevel? Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    [MaxLength(1000)]
    public string? ConfidenceComment { get; set; }
}
