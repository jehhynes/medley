using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for clustering fragments using K-means clustering
/// </summary>
public interface IFragmentClusteringService
{
    /// <summary>
    /// Performs K-means clustering on fragments
    /// </summary>
    /// <param name="session">The clustering session configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of created clusters</returns>
    Task<IEnumerable<Cluster>> PerformClusteringAsync(
        ClusteringSession session, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates cluster centroids and cohesion metrics
    /// </summary>
    /// <param name="cluster">The cluster to calculate metrics for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CalculateClusterMetricsAsync(
        Cluster cluster, 
        CancellationToken cancellationToken = default);
}
