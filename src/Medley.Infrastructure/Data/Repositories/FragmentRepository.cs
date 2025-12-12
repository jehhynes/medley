using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Fragment entity with vector similarity operations using EF Core
/// </summary>
public class FragmentRepository : Repository<Fragment>, IFragmentRepository
{
    private readonly ApplicationDbContext _context;

    public FragmentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds fragments similar to the given embedding vector using L2 distance
    /// </summary>
    public async Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null, CancellationToken cancellationToken = default)
    {
        var vector = new Vector(embedding);

        var query = _context.Fragments
            .Where(f => f.Embedding != null);

        if (threshold.HasValue)
        {
            query = query.Where(f => f.Embedding!.CosineDistance(vector) <= threshold.Value);
        }

        var results = await query
            .OrderBy(f => f.Embedding!.CosineDistance(vector))
            .Take(limit)
            .Select(f => new { Fragment = f, Distance = f.Embedding!.CosineDistance(vector) })
            .ToListAsync(cancellationToken);

        return results.Select(r => new FragmentSimilarityResult
        {
            RelatedEntity = r.Fragment,
            Distance = r.Distance
        });
    }
}
