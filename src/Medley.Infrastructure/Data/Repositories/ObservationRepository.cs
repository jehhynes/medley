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

    public async Task<IEnumerable<ObservationSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null)
    {
        var vector = new Vector(embedding);

        var query = _context.Observations
            .Where(o => o.Embedding != null);

        if (threshold.HasValue)
        {
            query = query.Where(o => o.Embedding!.CosineDistance(vector) <= threshold.Value);
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

