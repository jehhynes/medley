using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Provides metadata extraction from source metadata (attendees, folders, etc.)
/// </summary>
public interface ISourceMetadataProvider
{
    /// <summary>
    /// Extracts attendee emails from a source's metadata
    /// </summary>
    /// <param name="source">The source to extract attendees from</param>
    /// <returns>List of attendee email addresses</returns>
    IReadOnlyList<string> GetAttendeeEmails(Source source);

    /// <summary>
    /// Gets the folder path for a Google Drive source
    /// </summary>
    /// <param name="source">The source to extract folder information from</param>
    /// <returns>Array of folder names representing the path, or empty array if not available</returns>
    IReadOnlyList<string> GetFolders(Source source);
}
