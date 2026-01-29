using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for KnowledgeUnit entity with vector similarity operations using EF Core
/// </summary>
public class KnowledgeUnitRepository : Repository<KnowledgeUnit>, IKnowledgeUnitRepository
{
    private readonly ApplicationDbContext _context;

    public KnowledgeUnitRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds knowledge units similar to the given embedding vector using cosine distance
    /// </summary>
    public async Task<IEnumerable<KnowledgeUnitSimilarityResult>> FindSimilarAsync(
        float[] embedding, 
        int limit, 
        double? minSimilarity = null,
        CancellationToken cancellationToken = default,
        Func<IQueryable<KnowledgeUnit>, IQueryable<KnowledgeUnit>>? filter = null)
    {
        var vector = new Vector(embedding);

        IQueryable<KnowledgeUnit> query = _context.KnowledgeUnits
            .Include(ku => ku.Category);

        if (filter != null)
            query = filter(query);

        query = query.Where(ku => ku.Embedding != null);

        if (minSimilarity.HasValue)
        {
            // Convert similarity score (0-1) to cosine distance (0-2)
            // similarity = 1 - (distance / 2), so distance = (1 - similarity) * 2
            var maxDistance = (1 - minSimilarity.Value) * 2;
            query = query.Where(ku => ku.Embedding!.CosineDistance(vector) <= maxDistance);
        }

        var results = await query
            .OrderBy(ku => ku.Embedding!.CosineDistance(vector))
            .Take(limit)
            .Select(ku => new { KnowledgeUnit = ku, Distance = ku.Embedding!.CosineDistance(vector) })
            .ToListAsync(cancellationToken);

        return results.Select(r => new KnowledgeUnitSimilarityResult
        {
            RelatedEntity = r.KnowledgeUnit,
            Distance = r.Distance
        });
    }

    /// <summary>
    /// Gets a knowledge unit with its fragments loaded
    /// </summary>
    public async Task<KnowledgeUnit?> GetWithFragmentsAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        return await _context.KnowledgeUnits
            .Include(ku => ku.Fragments)
            .Include(ku => ku.Category)
            .FirstOrDefaultAsync(ku => ku.Id == id, cancellationToken);
    }
}
