using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a cluster of related fragments from a clustering session
/// </summary>
public class Cluster : BusinessEntity
{
    /// <summary>
    /// The clustering session this cluster belongs to
    /// </summary>
    public Guid ClusteringSessionId { get; set; }

    /// <summary>
    /// Navigation to the clustering session
    /// </summary>
    [ForeignKey(nameof(ClusteringSessionId))]
    public required virtual ClusteringSession ClusteringSession { get; set; }

    /// <summary>
    /// Sequential cluster number within the session (1, 2, 3, ...)
    /// </summary>
    public int ClusterNumber { get; set; }

    /// <summary>
    /// Number of fragments in this cluster
    /// </summary>
    public int FragmentCount { get; set; }

    /// <summary>
    /// Centroid vector (average embedding of all fragments in cluster)
    /// </summary>
    [Column(TypeName = "vector(2000)")]
    public Vector? Centroid { get; set; }

    /// <summary>
    /// Average intra-cluster distance (cohesion metric - lower is better)
    /// </summary>
    public double? IntraClusterDistance { get; set; }

    /// <summary>
    /// Fragments in this cluster (many-to-many)
    /// </summary>
    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
}
