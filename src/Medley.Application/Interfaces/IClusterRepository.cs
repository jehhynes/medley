using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Repository interface for Cluster entity
/// </summary>
public interface IClusterRepository : IRepository<Cluster>
{
    /// <summary>
    /// Gets all clusters for a specific clustering session
    /// </summary>
    /// <param name="clusteringSessionId">The clustering session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of clusters</returns>
    Task<IEnumerable<Cluster>> GetByClusteringSessionIdAsync(
        Guid clusteringSessionId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a cluster with its fragments loaded
    /// </summary>
    /// <param name="id">The cluster ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cluster with fragments if found, null otherwise</returns>
    Task<Cluster?> GetWithFragmentsAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
}
