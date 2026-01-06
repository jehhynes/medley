using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

public class ObservationRepository : Repository<Observation>, IObservationRepository
{
    private readonly ApplicationDbContext _context;

    public ObservationRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ObservationSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? minSimilarity = null)
    {
        var vector = new Vector(embedding);

        var query = _context.Observations
            .Where(o => o.Embedding != null);

        if (minSimilarity.HasValue)
        {
            // Convert similarity score (0-1) to cosine distance (0-2)
            // similarity = 1 - (distance / 2), so distance = (1 - similarity) * 2
            var maxDistance = (1 - minSimilarity.Value) * 2;
            query = query.Where(o => o.Embedding!.CosineDistance(vector) <= maxDistance);
        }

        var results = await query
            .OrderBy(o => o.Embedding!.CosineDistance(vector))
            .Take(limit)
            .Select(o => new { Observation = o, Distance = o.Embedding!.CosineDistance(vector) })
            .ToListAsync();

        return results.Select(r => new ObservationSimilarityResult
        {
            RelatedEntity = r.Observation,
            Distance = r.Distance
        });
    }
}

