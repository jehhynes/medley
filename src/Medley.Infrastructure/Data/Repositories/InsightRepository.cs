using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Medley.Infrastructure.Data.Repositories;

public class InsightRepository : Repository<Insight>, IInsightRepository
{
    public InsightRepository(ApplicationDbContext context) : base(context)
    {
    }
}