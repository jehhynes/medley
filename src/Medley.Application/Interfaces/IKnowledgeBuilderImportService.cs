using Medley.Application.Models;
using Medley.Domain.Models;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service for importing Knowledge Builder articles from JSON or ZIP files
/// </summary>
public interface IKnowledgeBuilderImportService
{
    /// <summary>
    /// Validates JSON structure from a stream
    /// </summary>
    /// <param name="jsonStream">Stream containing JSON data</param>
    /// <returns>Validation result with any errors</returns>
    Task<KnowledgeBuilderImportValidation> ValidateJsonAsync(Stream jsonStream);

    /// <summary>
    /// Imports articles from Knowledge Builder format
    /// </summary>
    /// <param name="articles">Array of articles to import</param>
    /// <param name="preserveHierarchy">Whether to maintain parent-child relationships</param>
    /// <returns>Import result with success count and errors</returns>
    Task<KnowledgeBuilderImportResult> ImportArticlesAsync(KnowledgeBuilderArticle[] articles, bool preserveHierarchy = true);

    /// <summary>
    /// Extracts JSON files from a ZIP archive
    /// </summary>
    /// <param name="zipStream">Stream containing ZIP data</param>
    /// <returns>List of JSON file contents</returns>
    Task<List<Stream>> ExtractZipAsync(Stream zipStream);

    /// <summary>
    /// Processes a file (JSON or ZIP) and imports the articles
    /// </summary>
    /// <param name="fileStream">File stream to process</param>
    /// <param name="fileName">Name of the file</param>
    /// <returns>Import result with success count and errors</returns>
    Task<KnowledgeBuilderImportResult> ProcessFileAsync(Stream fileStream, string fileName);
}

