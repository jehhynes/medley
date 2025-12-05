using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using System.Text.Json;

namespace Medley.Application.Services;

/// <summary>
/// Provides metadata extraction from source metadata (attendees, folders, etc.)
/// </summary>
public class SourceMetadataProvider : ISourceMetadataProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Extracts attendee emails from a source's metadata
    /// </summary>
    /// <param name="source">The source to extract attendees from</param>
    /// <returns>List of attendee email addresses</returns>
    public IReadOnlyList<string> GetAttendeeEmails(Source source)
    {
        if (source == null)
            return Array.Empty<string>();

        // Only process Collector_Fellow metadata type
        if (source.MetadataType != SourceMetadataType.Collector_Fellow)
            return Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(source.MetadataJson))
            return Array.Empty<string>();

        try
        {
            var recording = JsonSerializer.Deserialize<FellowRecordingImportModel>(source.MetadataJson, _jsonOptions);
            
            if (recording?.Note?.EventAttendees == null || recording.Note.EventAttendees.Count == 0)
                return Array.Empty<string>();

            // Extract emails from attendees, filtering out null/empty values
            var emails = recording.Note.EventAttendees
                .Where(a => !string.IsNullOrWhiteSpace(a.Email))
                .Select(a => a.Email!)
                .ToList();

            return emails;
        }
        catch (JsonException)
        {
            // If deserialization fails, return empty list
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the folder path for a Google Drive source
    /// </summary>
    /// <param name="source">The source to extract folder information from</param>
    /// <returns>Array of folder names representing the path, or empty array if not available</returns>
    public IReadOnlyList<string> GetFolders(Source source)
    {
        if (source == null)
            return Array.Empty<string>();

        // Only process Collector_GoogleDrive metadata type
        if (source.MetadataType != SourceMetadataType.Collector_GoogleDrive)
            return Array.Empty<string>();

        if (string.IsNullOrWhiteSpace(source.MetadataJson))
            return Array.Empty<string>();

        try
        {
            var video = JsonSerializer.Deserialize<GoogleDriveVideoImportModel>(source.MetadataJson, _jsonOptions);
            
            if (video?.FolderPath == null || video.FolderPath.Length == 0)
                return Array.Empty<string>();

            // Return the folder path, filtering out null/empty values
            var folders = video.FolderPath.ToList();

            return folders;
        }
        catch (JsonException)
        {
            // If deserialization fails, return empty list
            return Array.Empty<string>();
        }
    }
}
