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
    /// Gets a fragment by ID, respecting the global query filter for IsDeleted
    /// </summary>
    public override async Task<Fragment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Use FirstOrDefaultAsync instead of FindAsync because FindAsync bypasses query filters
        return await _context.Fragments
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    /// <summary>
    /// Finds fragments similar to the given embedding vector using cosine distance
    /// </summary>
    public async Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(
        float[] embedding, 
        int limit, 
        double? minSimilarity = null,
        bool excludeClustered = false,
        CancellationToken cancellationToken = default, 
        Func<IQueryable<Fragment>, IQueryable<Fragment>>? filter = null)
    {
        var vector = new Vector(embedding);

        IQueryable<Fragment> query = _context.Fragments
            .Include(f => f.FragmentCategory);

        if (filter != null)
            query = filter(query);

        query = query.Where(f => f.Embedding != null);

        // Exclude fragments that are already assigned to a KnowledgeUnit if requested
        if (excludeClustered)
        {
            query = query.Where(f => f.KnowledgeUnitId == null);
        }

        if (minSimilarity.HasValue)
        {
            // Convert similarity score (0-1) to cosine distance (0-2)
            // similarity = 1 - (distance / 2), so distance = (1 - similarity) * 2
            var maxDistance = (1 - minSimilarity.Value) * 2;
            query = query.Where(f => f.Embedding!.CosineDistance(vector) <= maxDistance);
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

    /// <summary>
    /// Gets fragments that belong to a specific KnowledgeUnit
    /// </summary>
    public async Task<IEnumerable<Fragment>> GetByKnowledgeUnitIdAsync(
        Guid knowledgeUnitId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Fragments
            .Include(f => f.FragmentCategory)
            .Where(f => f.KnowledgeUnitId == knowledgeUnitId)
            .ToListAsync(cancellationToken);
    }
}
