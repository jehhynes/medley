using Medley.Domain.Entities;
using Medley.Domain.Models;

namespace Medley.Application.Interfaces;

/// <summary>
/// Repository interface for Observation with vector operations
/// </summary>
public interface IObservationRepository : IRepository<Observation>
{
    /// <summary>
    /// Finds observations similar to the given embedding vector using cosine distance
    /// </summary>
    /// <param name="embedding">The embedding vector to compare against</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minSimilarity">Optional minimum similarity score threshold (0-1, where 1 is most similar)</param>
    /// <returns>Collection of similar observations with similarity scores ordered by similarity</returns>
    Task<IEnumerable<ObservationSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? minSimilarity = null);
}
