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
    /// The ID of the fragment category
    /// </summary>
    public Guid FragmentCategoryId { get; set; }

    /// <summary>
    /// Navigation to the fragment category
    /// </summary>
    [ForeignKey(nameof(FragmentCategoryId))]
    public required virtual FragmentCategory FragmentCategory { get; set; }

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
    /// Message or reasoning from the clustering process
    /// </summary>
    [MaxLength(2000)]
    public string? ClusteringMessage { get; set; }

    /// <summary>
    /// The ID of the representative fragment for this cluster
    /// </summary>
    public Guid? RepresentativeFragmentId { get; set; }

    /// <summary>
    /// Navigation to the representative fragment
    /// </summary>
    [ForeignKey(nameof(RepresentativeFragmentId))]
    public virtual Fragment? RepresentativeFragment { get; set; }

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
    /// Plan fragments that reference this fragment
    /// </summary>
    public virtual ICollection<PlanFragment> PlanFragments { get; set; } = new List<PlanFragment>();
}
