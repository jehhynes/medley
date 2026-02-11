using Hangfire;
using Medley.Application.Integrations.Interfaces;
using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Medley.Application.Integrations.Services;

/// <summary>
/// Service for importing sources from Collector utility exports
/// </summary>
public class CollectorImportService : ICollectorImportService
{
    private readonly IRepository<Source> _sourceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CollectorImportService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CollectorImportService(
        IRepository<Source> sourceRepository,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CollectorImportService> logger)
    {
        _sourceRepository = sourceRepository;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    private const int MinContentLength = 100;

    /// <summary>
    /// Imports a single Google Drive video as a Source
    /// </summary>
    /// <returns>The created Source, or null if skipped due to missing/short content</returns>
    public async Task<Source?> ImportGoogleDriveVideoAsync(GoogleDriveVideoImportModel video, Integration integration)
    {
        if (string.IsNullOrWhiteSpace(video.Id))
        {
            throw new ArgumentException("Google Drive video must have an Id", nameof(video));
        }

        // Check if source already exists
        var existingSource = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.ExternalId == video.Id);

        if (existingSource != null)
        {
            _logger.LogDebug("Source already exists for Google Drive video {VideoId}. Skipping.", video.Id);
            return existingSource;
        }

        // Consolidate transcript
        string? consolidatedTranscript = null;
        if (video.Transcript != null && video.Transcript.Count > 0)
        {
            consolidatedTranscript = ConsolidateGoogleTranscript(video.Transcript);
        }

        // Skip if content is null or too short
        if (string.IsNullOrWhiteSpace(consolidatedTranscript) || consolidatedTranscript.Length < MinContentLength)
        {
            _logger.LogDebug("Skipping Google Drive video {VideoId} ({Name}): content is null or too short ({Length} chars)",
                video.Id, video.Name, consolidatedTranscript?.Length ?? 0);
            return null;
        }

        string metadataJson = JsonSerializer.Serialize(video);

        // Create source
        var source = new Source
        {
            Type = SourceType.Meeting,
            Name = video.Name,
            ExternalId = video.Id,
            Content = consolidatedTranscript,
            MetadataJson = metadataJson,
            MetadataType = SourceMetadataType.Collector_GoogleDrive,
            Date = video.CreatedTime.HasValue ? new DateTimeOffset(video.CreatedTime.Value.ToUniversalTime(), TimeSpan.Zero) : DateTimeOffset.UtcNow,
            Integration = integration
        };

        await _sourceRepository.Add(source);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created source for Google Drive video {VideoId} ({Name})",
            video.Id, video.Name);

        // Trigger smart tag processing for the new source
        _backgroundJobClient.Schedule<SmartTagProcessorJob>(
            j => j.ExecuteAsync(default!, default, source.Id), TimeSpan.FromMinutes(10));
        _logger.LogInformation("Enqueued smart tag processing job for source {SourceId}", source.Id);

        return source;
    }

    /// <summary>
    /// Imports a single Fellow recording as a Source
    /// </summary>
    /// <returns>The created Source, or null if skipped due to missing/short content</returns>
    public async Task<Source?> ImportFellowRecordingAsync(FellowRecordingImportModel recording, Integration integration)
    {
        if (string.IsNullOrWhiteSpace(recording.Id))
        {
            throw new ArgumentException("Fellow recording must have an Id", nameof(recording));
        }

        // Check if source already exists
        var existingSource = await _sourceRepository.Query()
            .FirstOrDefaultAsync(s => s.ExternalId == recording.Id);

        if (existingSource != null)
        {
            _logger.LogDebug("Source already exists for Fellow recording {RecordingId}. Skipping.", recording.Id);
            return existingSource;
        }

        // Consolidate transcript
        string? consolidatedTranscript = null;
        if (recording.Transcript?.SpeechSegments != null && recording.Transcript.SpeechSegments.Count > 0)
        {
            consolidatedTranscript = ConsolidateTranscriptBySpeaker(recording.Transcript.SpeechSegments);
        }

        // Skip if content is null or too short
        if (string.IsNullOrWhiteSpace(consolidatedTranscript) || consolidatedTranscript.Length < MinContentLength)
        {
            _logger.LogDebug("Skipping Fellow recording {RecordingId} ({Title}): content is null or too short ({Length} chars)",
                recording.Id, recording.Title, consolidatedTranscript?.Length ?? 0);
            return null;
        }

        string metadataJson = JsonSerializer.Serialize(recording);

        // Create source
        var source = new Source
        {
            Type = SourceType.Meeting,
            Name = recording.Title,
            ExternalId = recording.Id,
            Content = consolidatedTranscript,
            MetadataJson = metadataJson,
            MetadataType = SourceMetadataType.Collector_Fellow,
            Date = recording.StartedAt.HasValue ? new DateTimeOffset(recording.StartedAt.Value.ToUniversalTime(), TimeSpan.Zero) : DateTimeOffset.UtcNow,
            Integration = integration
        };

        await _sourceRepository.Add(source);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created source for Fellow recording {RecordingId} ({Title})",
            recording.Id, recording.Title);

        // Trigger smart tag processing for the new source
        _backgroundJobClient.Schedule<SmartTagProcessorJob>(
            j => j.ExecuteAsync(default!, default, source.Id), TimeSpan.FromSeconds(2));
        _logger.LogInformation("Enqueued smart tag processing job for source {SourceId}", source.Id);

        return source;
    }

    /// <summary>
    /// Imports multiple Google Drive videos from JSON (single video or array)
    /// </summary>
    public async Task<List<Source>> ImportGoogleDriveVideosFromJsonAsync(string json, Integration integration)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content cannot be empty", nameof(json));
        }

        var sources = new List<Source>();

        try
        {
            // Try to deserialize as single object first
            var video = JsonSerializer.Deserialize<GoogleDriveVideoImportModel>(json, _jsonOptions);
            if (video != null)
            {
                var source = await ImportGoogleDriveVideoAsync(video, integration);
                if (source != null)
                {
                    sources.Add(source);
                }
                return sources;
            }
        }
        catch (JsonException)
        {
            // If deserialization fails, try array
        }

        try
        {
            // Try to deserialize as array
            var videos = JsonSerializer.Deserialize<List<GoogleDriveVideoImportModel>>(json, _jsonOptions);
            if (videos != null && videos.Count > 0)
            {
                foreach (var video in videos)
                {
                    var source = await ImportGoogleDriveVideoAsync(video, integration);
                    if (source != null)
                    {
                        sources.Add(source);
                    }
                }
                return sources;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Google Drive video JSON");
            throw new ArgumentException("Invalid JSON format for Google Drive video(s)", nameof(json), ex);
        }

        throw new ArgumentException("JSON does not contain valid Google Drive video data", nameof(json));
    }

    /// <summary>
    /// Imports multiple Fellow recordings from JSON (single recording or array)
    /// </summary>
    public async Task<List<Source>> ImportFellowRecordingsFromJsonAsync(string json, Integration integration)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON content cannot be empty", nameof(json));
        }

        var sources = new List<Source>();

        try
        {
            // Try to deserialize as single object first
            var recording = JsonSerializer.Deserialize<FellowRecordingImportModel>(json, _jsonOptions);
            if (recording != null)
            {
                var source = await ImportFellowRecordingAsync(recording, integration);
                if (source != null)
                {
                    sources.Add(source);
                }
                return sources;
            }
        }
        catch (JsonException)
        {
            // If deserialization fails, try array
        }

        try
        {
            // Try to deserialize as array
            var recordings = JsonSerializer.Deserialize<List<FellowRecordingImportModel>>(json, _jsonOptions);
            if (recordings != null && recordings.Count > 0)
            {
                foreach (var recording in recordings)
                {
                    var source = await ImportFellowRecordingAsync(recording, integration);
                    if (source != null)
                    {
                        sources.Add(source);
                    }
                }
                return sources;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Fellow recording JSON");
            throw new ArgumentException("Invalid JSON format for Fellow recording(s)", nameof(json), ex);
        }

        throw new ArgumentException("JSON does not contain valid Fellow recording data", nameof(json));
    }

    /// <summary>
    /// Validates JSON structure from a stream
    /// </summary>
    public async Task<SourceImportValidation> ValidateJsonAsync(Stream jsonStream, SourceMetadataType metadataType)
    {
        try
        {
            jsonStream.Position = 0;
            var sourceCount = 0;
            var errors = new List<string>();
            var warnings = new List<string>();

            if (metadataType == SourceMetadataType.Collector_GoogleDrive)
            {
                // Try to deserialize as Google Drive video(s)
                try
                {
                    var videos = await JsonSerializer.DeserializeAsync<List<GoogleDriveVideoImportModel>>(jsonStream, _jsonOptions);
                    if (videos != null && videos.Count > 0)
                    {
                        sourceCount = videos.Count;
                        ValidateGoogleDriveVideos(videos, errors, warnings);
                    }
                }
                catch (JsonException)
                {
                    // Try single object
                    jsonStream.Position = 0;
                    var video = await JsonSerializer.DeserializeAsync<GoogleDriveVideoImportModel>(jsonStream, _jsonOptions);
                    if (video != null)
                    {
                        sourceCount = 1;
                        ValidateGoogleDriveVideos(new List<GoogleDriveVideoImportModel> { video }, errors, warnings);
                    }
                }
            }
            else if (metadataType == SourceMetadataType.Collector_Fellow)
            {
                // Try to deserialize as Fellow recording(s)
                try
                {
                    var recordings = await JsonSerializer.DeserializeAsync<List<FellowRecordingImportModel>>(jsonStream, _jsonOptions);
                    if (recordings != null && recordings.Count > 0)
                    {
                        sourceCount = recordings.Count;
                        ValidateFellowRecordings(recordings, errors, warnings);
                    }
                }
                catch (JsonException)
                {
                    // Try single object
                    jsonStream.Position = 0;
                    var recording = await JsonSerializer.DeserializeAsync<FellowRecordingImportModel>(jsonStream, _jsonOptions);
                    if (recording != null)
                    {
                        sourceCount = 1;
                        ValidateFellowRecordings(new List<FellowRecordingImportModel> { recording }, errors, warnings);
                    }
                }
            }
            else
            {
                return SourceImportValidation.Invalid("Invalid metadata type specified");
            }

            if (sourceCount == 0)
            {
                return SourceImportValidation.Invalid("No valid sources found in JSON file");
            }

            if (errors.Any())
            {
                return new SourceImportValidation
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            return new SourceImportValidation
            {
                IsValid = true,
                SourceCount = sourceCount,
                Warnings = warnings
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed");
            return SourceImportValidation.Invalid($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation failed");
            return SourceImportValidation.Invalid($"Validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Extracts JSON files from a ZIP archive
    /// </summary>
    public async Task<List<Stream>> ExtractZipAsync(Stream zipStream)
    {
        var jsonStreams = new List<Stream>();

        try
        {
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    var memoryStream = new MemoryStream();
                    using (var entryStream = entry.Open())
                    {
                        await entryStream.CopyToAsync(memoryStream);
                    }
                    memoryStream.Position = 0;
                    jsonStreams.Add(memoryStream);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract ZIP file");
            throw;
        }

        return jsonStreams;
    }

    /// <summary>
    /// Processes a file (JSON or ZIP) and imports the sources
    /// </summary>
    public async Task<SourceImportResult> ProcessFileAsync(Stream fileStream, string fileName, SourceMetadataType metadataType, Integration integration)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var allSources = new List<Source>();
            var allErrors = new List<string>();
            var allWarnings = new List<string>();

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processing ZIP file: {FileName}", fileName);
                var jsonStreams = await ExtractZipAsync(fileStream);

                if (!jsonStreams.Any())
                {
                    return SourceImportResult.FailureResult("No JSON files found in ZIP archive");
                }

                foreach (var jsonStream in jsonStreams)
                {
                    try
                    {
                        var json = await new StreamReader(jsonStream).ReadToEndAsync();
                        
                        List<Source> sources;
                        if (metadataType == SourceMetadataType.Collector_GoogleDrive)
                        {
                            sources = await ImportGoogleDriveVideosFromJsonAsync(json, integration);
                        }
                        else if (metadataType == SourceMetadataType.Collector_Fellow)
                        {
                            sources = await ImportFellowRecordingsFromJsonAsync(json, integration);
                        }
                        else
                        {
                            allErrors.Add("Invalid metadata type specified");
                            continue;
                        }

                        allSources.AddRange(sources);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process JSON from ZIP");
                        allErrors.Add($"Failed to process JSON: {ex.Message}");
                    }
                    finally
                    {
                        await jsonStream.DisposeAsync();
                    }
                }
            }
            else if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Processing JSON file: {FileName}", fileName);
                var json = await new StreamReader(fileStream).ReadToEndAsync();

                List<Source> sources;
                if (metadataType == SourceMetadataType.Collector_GoogleDrive)
                {
                    sources = await ImportGoogleDriveVideosFromJsonAsync(json, integration);
                }
                else if (metadataType == SourceMetadataType.Collector_Fellow)
                {
                    sources = await ImportFellowRecordingsFromJsonAsync(json, integration);
                }
                else
                {
                    return SourceImportResult.FailureResult("Invalid metadata type specified");
                }

                allSources.AddRange(sources);
            }
            else
            {
                return SourceImportResult.FailureResult("Unsupported file type. Only .json and .zip files are supported.");
            }

            if (!allSources.Any() && !allErrors.Any())
            {
                return SourceImportResult.FailureResult("No valid sources found in file");
            }

            var duration = DateTime.UtcNow - startTime;

            // Count how many were actually new vs skipped (existing)
            var imported = allSources.Count;
            var skipped = 0; // Track if needed

            return new SourceImportResult
            {
                Success = !allErrors.Any(),
                SourcesImported = imported,
                SourcesSkipped = skipped,
                TotalSourcesProcessed = imported + skipped,
                Errors = allErrors,
                Warnings = allWarnings,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process file: {FileName}", fileName);
            return SourceImportResult.FailureResult($"Processing error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates Google Drive videos
    /// </summary>
    private void ValidateGoogleDriveVideos(List<GoogleDriveVideoImportModel> videos, List<string> errors, List<string> warnings)
    {
        foreach (var video in videos)
        {
            if (string.IsNullOrWhiteSpace(video.Id))
            {
                errors.Add($"Video '{video.Name}' has no ID");
            }

            if (string.IsNullOrWhiteSpace(video.Name))
            {
                warnings.Add("Video has no name specified");
            }
        }
    }

    /// <summary>
    /// Validates Fellow recordings
    /// </summary>
    private void ValidateFellowRecordings(List<FellowRecordingImportModel> recordings, List<string> errors, List<string> warnings)
    {
        foreach (var recording in recordings)
        {
            if (string.IsNullOrWhiteSpace(recording.Id))
            {
                errors.Add($"Recording '{recording.Title}' has no ID");
            }

            if (string.IsNullOrWhiteSpace(recording.Title))
            {
                warnings.Add("Recording has no title specified");
            }
        }
    }

    /// <summary>
    /// Consolidates Google Drive transcript segments into a readable format
    /// </summary>
    private static string ConsolidateGoogleTranscript(List<GoogleTranscriptSegmentImportModel> segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new StringBuilder();

        foreach (var segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                result.Append(segment.Text.Trim());
                result.Append(' ');
            }
        }

        return result.ToString().TrimEnd();
    }

    /// <summary>
    /// Consolidates transcript segments by speaker, combining consecutive segments from the same speaker
    /// Ported from Medley.Collector/TranscriptViewerForm.cs (lines 87-126)
    /// </summary>
    private static string ConsolidateTranscriptBySpeaker(List<FellowSpeechSegmentImportModel> segments)
    {
        if (segments == null || segments.Count == 0)
            return string.Empty;

        var result = new StringBuilder();
        string? currentSpeaker = null;
        var currentTexts = new List<string>();

        foreach (var segment in segments)
        {
            if (segment.Speaker != currentSpeaker)
            {
                // Write out the previous speaker's consolidated text
                if (currentSpeaker != null && currentTexts.Count > 0)
                {
                    result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
                    result.AppendLine(); // Add blank line between speakers
                }

                // Start new speaker
                currentSpeaker = segment.Speaker;
                currentTexts.Clear();
            }

            // Add text to current speaker's segments
            if (!string.IsNullOrWhiteSpace(segment.Text))
            {
                currentTexts.Add(segment.Text.Trim());
            }
        }

        // Write out the last speaker's text
        if (currentSpeaker != null && currentTexts.Count > 0)
        {
            result.AppendLine($"{currentSpeaker}: {string.Join(" ", currentTexts)}");
        }

        return result.ToString().TrimEnd();
    }
}

