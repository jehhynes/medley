using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using System.Text.Json;

namespace Medley.Application.Services;

/// <summary>
/// Provides attendee email extraction from source metadata
/// </summary>
public class SourceAttendeeProvider : ISourceAttendeeProvider
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
}
