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
    /// <param name="embedding">The embedding vector to compare against (1536 dimensions)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="threshold">Optional maximum cosine distance threshold (0-2, lower is more similar)</param>
    /// <returns>Collection of similar fragments with similarity scores ordered by similarity</returns>
    Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null);

    /// <summary>
    /// Gets a fragment by its unique identifier
    /// </summary>
    /// <param name="id">The fragment identifier</param>
    /// <returns>The fragment if found, null otherwise</returns>
    Task<Fragment?> GetByIdAsync(Guid id);
}
