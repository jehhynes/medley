using Medley.Application.Models;
using Medley.Domain.Entities;

namespace Medley.Application.Integrations.Interfaces;

public interface IYouTubeImportService
{
    /// <summary>
    /// Imports multiple YouTube videos by URL
    /// </summary>
    Task<SourceImportResult> ImportAsync(IEnumerable<string> urls, Integration integration);

    /// <summary>
    /// Imports a single YouTube video by URL. Returns null if skipped (already exists or no transcript).
    /// </summary>
    Task<Source?> ImportSingleAsync(string url, Integration integration);

    /// <summary>
    /// Searches YouTube via SocialKit, then imports up to <paramref name="maxResults"/> videos.
    /// Videos whose ExternalId already exists in the database are skipped.
    /// </summary>
    Task<SourceImportResult> SearchAndImportAsync(string query, int maxResults, Integration integration);
}
