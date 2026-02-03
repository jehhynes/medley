using Medley.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a clustering session that groups fragments using a specific algorithm
/// </summary>
public class ClusteringSession : BusinessEntity
{
    /// <summary>
    /// The clustering algorithm method used
    /// </summary>
    public ClusteringMethod Method { get; set; }

    /// <summary>
    /// The linkage type for hierarchical clustering
    /// </summary>
    public LinkageType? Linkage { get; set; }

    /// <summary>
    /// The distance metric used for clustering
    /// </summary>
    public DistanceMetric DistanceMetric { get; set; } = DistanceMetric.Cosine;

    /// <summary>
    /// Distance threshold for cutting the dendrogram (lower = more similar required)
    /// </summary>
    public double? DistanceThreshold { get; set; }

    /// <summary>
    /// Minimum number of fragments required per cluster
    /// </summary>
    public int MinClusterSize { get; set; } = 2;

    /// <summary>
    /// Maximum number of fragments allowed per cluster (optional)
    /// </summary>
    public int? MaxClusterSize { get; set; }

    /// <summary>
    /// Total number of fragments processed in this session
    /// </summary>
    public int FragmentCount { get; set; }

    /// <summary>
    /// Number of clusters created in this session
    /// </summary>
    public int ClusterCount { get; set; }

    /// <summary>
    /// Current status of the clustering session
    /// </summary>
    public ClusteringStatus Status { get; set; } = ClusteringStatus.Pending;

    /// <summary>
    /// Status message (error details or completion summary)
    /// </summary>
    [MaxLength(2000)]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// When the clustering session completed (success or failure)
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Clusters created in this session
    /// </summary>
    public virtual ICollection<Cluster> Clusters { get; set; } = new List<Cluster>();
}
