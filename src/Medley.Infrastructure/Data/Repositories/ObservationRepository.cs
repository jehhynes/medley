using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector;

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
        var idsWithDistances = new List<(Guid Id, double Distance)>();

        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        var shouldCloseConnection = false;
        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
                shouldCloseConnection = true;
            }
            await connection.ReloadTypesAsync();

            var sql = "SELECT \"Id\", \"Embedding\"::vector <-> @embedding AS distance " +
                      "FROM \"Observations\" " +
                      "WHERE \"Embedding\" IS NOT NULL ";
            if (threshold.HasValue)
            {
                sql += "AND \"Embedding\"::vector <-> @embedding <= @threshold ";
            }
            sql += "ORDER BY \"Embedding\"::vector <-> @embedding LIMIT @limit";

            await using (var cmd = new NpgsqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@embedding", new Vector(embedding));
                cmd.Parameters.AddWithValue("@limit", limit);
                if (threshold.HasValue)
                {
                    cmd.Parameters.AddWithValue("@threshold", threshold.Value);
                }
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    idsWithDistances.Add((reader.GetGuid(0), reader.GetDouble(1)));
                }
            }
        }
        finally
        {
            if (shouldCloseConnection && connection.State == System.Data.ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }

        if (idsWithDistances.Count == 0)
            return [];

        var ids = idsWithDistances.Select(x => x.Id).ToList();
        var frags = await _context.Observations
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id);

        return idsWithDistances
            .Where(x => frags.ContainsKey(x.Id))
            .Select(x => new ObservationSimilarityResult
            {
                RelatedEntity = frags[x.Id],
                Distance = x.Distance
            })
            .ToList();
    }
}

