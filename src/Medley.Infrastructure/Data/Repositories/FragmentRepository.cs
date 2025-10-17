using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

namespace Medley.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Fragment entity with vector similarity operations using raw Npgsql
/// </summary>
public class FragmentRepository : Repository<Fragment>, IFragmentRepository
{
    private readonly ApplicationDbContext _context;

    public FragmentRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds fragments similar to the given embedding vector using cosine distance
    /// </summary>
    public async Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null)
    {
        var idsWithDistances = new List<(Guid Id, double Distance)>();

        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();

        // Ensure connection is open
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        // Format embedding as PostgreSQL vector literal: '[1,2,3]'
        var vectorString = $"[{string.Join(",", embedding)}]";

        var sql = $@"SELECT ""Id"", ""Embedding""::vector <-> @embedding AS distance
                     FROM ""Fragments""
                     WHERE ""Embedding"" IS NOT NULL
                     {(threshold.HasValue ? $"AND \"Embedding\"::vector <-> @embedding <= {threshold.Value}" : "")}
                     ORDER BY ""Embedding""::vector <-> @embedding
                     LIMIT @limit";

        await using (var cmd = new NpgsqlCommand(sql, connection))
        {
            cmd.Parameters.AddWithValue("@embedding", new Vector(embedding));
            cmd.Parameters.AddWithValue("@limit", limit);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    idsWithDistances.Add((reader.GetGuid(0), reader.GetDouble(1)));
                }
            }
        }

        // Load full entities through EF in the same order
        if (idsWithDistances.Count == 0)
            return [];

        var ids = idsWithDistances.Select(x => x.Id).ToList();
        var fragments = await _context.Fragments
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id);

        // Maintain order and combine with distances
        return idsWithDistances
            .Where(x => fragments.ContainsKey(x.Id))
            .Select(x => new FragmentSimilarityResult
            {
                Fragment = fragments[x.Id],
                Distance = x.Distance
            })
            .ToList();
    }

    /// <summary>
    /// Gets a fragment by its unique identifier
    /// </summary>
    public async Task<Fragment?> GetByIdAsync(Guid id)
    {
        return await _context.Fragments.FindAsync(id);
    }
}
