using Medley.Application.Models;
using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for chunking content intelligently using AI and speech segments
/// </summary>
public interface IContentChunkingService
{
    /// <summary>
    /// Chunks content from a source using speech segments from metadata.
    /// Uses LLM to determine optimal segment groupings based on topical cohesion.
    /// </summary>
    /// <param name="source">The source containing content and speech segment metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of content chunks ready for processing</returns>
    /// <exception cref="InvalidOperationException">Thrown when source does not contain speech segments</exception>
    Task<List<ContentChunk>> ChunkContentAsync(Source source, CancellationToken cancellationToken = default);
}

