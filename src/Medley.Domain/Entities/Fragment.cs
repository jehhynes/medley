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
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the fragment
    /// </summary>
    [MaxLength(500)]
    public required string Summary { get; set; }

    /// <summary>
    /// The ID of the knowledge category
    /// </summary>
    public Guid KnowledgeCategoryId { get; set; }

    /// <summary>
    /// Navigation to the knowledge category
    /// </summary>
    [ForeignKey(nameof(KnowledgeCategoryId))]
    public required virtual KnowledgeCategory KnowledgeCategory { get; set; }

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
    /// Vector embedding specifically for clustering operations
    /// </summary>
    [Column(TypeName = "vector(2000)")]
    public Vector? ClusteringEmbedding { get; set; }

    /// <summary>
    /// The ID of the originating source
    /// </summary>
    public Guid? SourceId { get; set; }

    /// <summary>
    /// Navigation to the originating Source
    /// </summary>
    [ForeignKey(nameof(SourceId))]
    public virtual Source? Source { get; set; }

    /// <summary>
    /// Date when the fragment was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Date when the fragment was processed for clustering
    /// </summary>
    public DateTimeOffset? ClusteringProcessed { get; set; }

    /// <summary>
    /// Many-to-many relationships with knowledge units
    /// </summary>
    public virtual ICollection<FragmentKnowledgeUnit> FragmentKnowledgeUnits { get; set; } = new List<FragmentKnowledgeUnit>();

    /// <summary>
    /// Confidence level from the AI extraction
    /// </summary>
    public ConfidenceLevel? Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    [MaxLength(1000)]
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Indicates if this fragment has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Clusters this fragment belongs to (many-to-many)
    /// </summary>
    public virtual ICollection<Cluster> Clusters { get; set; } = new List<Cluster>();
}
