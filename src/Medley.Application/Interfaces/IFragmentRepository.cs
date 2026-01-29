using Medley.Domain.Entities;
using Medley.Domain.Models;

namespace Medley.Application.Interfaces;

/// <summary>
/// Repository interface for Fragment entity with vector similarity operations
/// </summary>
public interface IFragmentRepository : IRepository<Fragment>
{
    /// <summary>
    /// Finds fragments similar to the given embedding vector using cosine distance
    /// </summary>
    /// <param name="embedding">The embedding vector to compare against (2000 dimensions)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minSimilarity">Optional minimum similarity score threshold (0-1, where 1 is most similar)</param>
    /// <param name="excludeClustered">If true, excludes fragments that are already assigned to a KnowledgeUnit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="filter">Optional filter function to apply additional query constraints</param>
    /// <returns>Collection of similar fragments with similarity scores ordered by similarity</returns>
    Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(
        float[] embedding, 
        int limit, 
        double? minSimilarity = null, 
        bool excludeClustered = false,
        CancellationToken cancellationToken = default, 
        Func<IQueryable<Fragment>, IQueryable<Fragment>>? filter = null);

    /// <summary>
    /// Gets fragments that belong to a specific KnowledgeUnit
    /// </summary>
    /// <param name="knowledgeUnitId">The ID of the KnowledgeUnit to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of fragments belonging to the specified KnowledgeUnit</returns>
    Task<IEnumerable<Fragment>> GetByKnowledgeUnitIdAsync(
        Guid knowledgeUnitId, 
        CancellationToken cancellationToken = default);
}
