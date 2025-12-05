using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Provides attendee email extraction from source metadata
/// </summary>
public interface ISourceAttendeeProvider
{
    /// <summary>
    /// Extracts attendee emails from a source's metadata
    /// </summary>
    /// <param name="source">The source to extract attendees from</param>
    /// <returns>List of attendee email addresses</returns>
    IReadOnlyList<string> GetAttendeeEmails(Source source);
}
