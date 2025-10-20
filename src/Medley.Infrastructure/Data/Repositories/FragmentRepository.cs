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
    private readonly NpgsqlDataSource _dataSource;

    public FragmentRepository(ApplicationDbContext context, NpgsqlDataSource dataSource) : base(context)
    {
        _context = context;
        _dataSource = dataSource;
    }

    /// <summary>
    /// Finds fragments similar to the given embedding vector using cosine distance
    /// </summary>
    public async Task<IEnumerable<FragmentSimilarityResult>> FindSimilarAsync(float[] embedding, int limit, double? threshold = null)
    {
        var idsWithDistances = new List<(Guid Id, double Distance)>();

        // Get the connection from EF Core context (which uses the data source with vector support)
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        var shouldCloseConnection = false;

        try
        {
            // Ensure connection is open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }

            // Reload types to ensure vector extension is recognized
            await connection.ReloadTypesAsync();

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
        }
        finally
        {
            // Only close if we opened it
            if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
            {
                await connection.CloseAsync();
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
                RelatedEntity = fragments[x.Id],
                Distance = x.Distance
            })
            .ToList();
    }
}
