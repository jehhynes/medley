using Medley.Application.Interfaces;
using Medley.Domain.Entities;

namespace Medley.Infrastructure.Data.Repositories;

public class FindingRepository : Repository<Finding>, IFindingRepository
{
    private readonly ApplicationDbContext _context;

    public FindingRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }
}