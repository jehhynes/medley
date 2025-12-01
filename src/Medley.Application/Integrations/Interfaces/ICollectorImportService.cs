using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;

namespace Medley.Application.Integrations.Interfaces;

/// <summary>
/// Service for importing sources from Collector utility exports (Google Drive videos and Fellow recordings)
/// </summary>
public interface ICollectorImportService
{
    /// <summary>
    /// Imports a single Google Drive video as a Source
    /// </summary>
    /// <param name="video">Google Drive video data from Collector</param>
    /// <param name="integration">Integration this source belongs to</param>
    /// <returns>Created Source entity, or null if skipped due to missing/short content</returns>
    Task<Source?> ImportGoogleDriveVideoAsync(GoogleDriveVideoImportModel video, Integration integration);
    
    /// <summary>
    /// Imports a single Fellow recording as a Source
    /// </summary>
    /// <param name="recording">Fellow recording data from Collector</param>
    /// <param name="integration">Integration this source belongs to</param>
    /// <returns>Created Source entity, or null if skipped due to missing/short content</returns>
    Task<Source?> ImportFellowRecordingAsync(FellowRecordingImportModel recording, Integration integration);
    
    /// <summary>
    /// Imports multiple Google Drive videos from JSON (single video or array)
    /// </summary>
    /// <param name="json">JSON string containing video(s) from Collector</param>
    /// <param name="integration">Integration this source belongs to</param>
    /// <returns>List of created Source entities</returns>
    Task<List<Source>> ImportGoogleDriveVideosFromJsonAsync(string json, Integration integration);
    
    /// <summary>
    /// Imports multiple Fellow recordings from JSON (single recording or array)
    /// </summary>
    /// <param name="json">JSON string containing recording(s) from Collector</param>
    /// <param name="integration">Integration this source belongs to</param>
    /// <returns>List of created Source entities</returns>
    Task<List<Source>> ImportFellowRecordingsFromJsonAsync(string json, Integration integration);

    /// <summary>
    /// Validates JSON structure from a stream
    /// </summary>
    /// <param name="jsonStream">Stream containing JSON data</param>
    /// <param name="metadataType">Expected metadata type (GoogleDrive or Fellow)</param>
    /// <returns>Validation result with any errors</returns>
    Task<SourceImportValidation> ValidateJsonAsync(Stream jsonStream, SourceMetadataType metadataType);

    /// <summary>
    /// Extracts JSON files from a ZIP archive
    /// </summary>
    /// <param name="zipStream">Stream containing ZIP data</param>
    /// <returns>List of JSON file contents as streams</returns>
    Task<List<Stream>> ExtractZipAsync(Stream zipStream);

    /// <summary>
    /// Processes a file (JSON or ZIP) and imports the sources
    /// </summary>
    /// <param name="fileStream">File stream to process</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="metadataType">Type of metadata (GoogleDrive or Fellow)</param>
    /// <param name="integration">Integration this source belongs to</param>
    /// <returns>Import result with success count and errors</returns>
    Task<SourceImportResult> ProcessFileAsync(Stream fileStream, string fileName, SourceMetadataType metadataType, Integration integration);
}

