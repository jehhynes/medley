using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Cluster entity
/// </summary>
public class ClusterRepository : Repository<Cluster>, IClusterRepository
{
    public ClusterRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Cluster>> GetByClusteringSessionIdAsync(
        Guid clusteringSessionId,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(c => c.ClusteringSessionId == clusteringSessionId)
            .OrderBy(c => c.ClusterNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cluster?> GetWithFragmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Include(c => c.Fragments)
                .ThenInclude(f => f.KnowledgeCategory)
            .Include(c => c.Fragments)
                .ThenInclude(f => f.Source)
            .Include(c => c.ClusteringSession)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}
