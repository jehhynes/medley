using Medley.Domain.Entities;
using Medley.Domain.Models;

namespace Medley.Application.Interfaces;

/// <summary>
/// Repository interface for Observation with vector operations
/// </summary>
public interface IObservationRepository : IRepository<Observation>
{
    Task<IEnumerable<ObservationSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null);
}
