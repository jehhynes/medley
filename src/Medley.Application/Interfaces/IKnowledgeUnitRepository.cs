using Medley.Domain.Entities;
using Medley.Domain.Models;

namespace Medley.Application.Interfaces;

/// <summary>
/// Repository interface for KnowledgeUnit entity with vector similarity operations
/// </summary>
public interface IKnowledgeUnitRepository : IRepository<KnowledgeUnit>
{
    /// <summary>
    /// Finds knowledge units similar to the given embedding vector using cosine distance
    /// </summary>
    /// <param name="embedding">The embedding vector to compare against (2000 dimensions)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minSimilarity">Optional minimum similarity score threshold (0-1, where 1 is most similar)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="filter">Optional filter function to apply additional query constraints</param>
    /// <returns>Collection of similar knowledge units with similarity scores ordered by similarity</returns>
    Task<IEnumerable<KnowledgeUnitSimilarityResult>> FindSimilarAsync(
        float[] embedding, 
        int limit, 
        double? minSimilarity = null, 
        CancellationToken cancellationToken = default,
        Func<IQueryable<KnowledgeUnit>, IQueryable<KnowledgeUnit>>? filter = null);

    /// <summary>
    /// Gets a knowledge unit with its fragments loaded
    /// </summary>
    /// <param name="id">The knowledge unit identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The knowledge unit with fragments if found, null otherwise</returns>
    Task<KnowledgeUnit?> GetWithFragmentsAsync(
        Guid id, 
        CancellationToken cancellationToken = default);
}
