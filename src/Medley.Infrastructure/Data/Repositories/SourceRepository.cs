using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Source entity using EF Core
/// </summary>
public class SourceRepository : Repository<Source>, ISourceRepository
{
    private readonly ApplicationDbContext _context;

    public SourceRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets a source by ID, respecting the global query filter for IsDeleted
    /// </summary>
    public override async Task<Source?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Use FirstOrDefaultAsync instead of FindAsync because FindAsync bypasses query filters
        return await _context.Sources
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
