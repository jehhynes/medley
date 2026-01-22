using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a speaker extracted from meeting transcripts
/// </summary>
public class Speaker : BusinessEntity
{
    /// <summary>
    /// Name or identifier of the speaker (e.g., "John Doe", "Speaker 1")
    /// </summary>
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Email address of the speaker (if identified)
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether this speaker is internal to the organization
    /// null = not yet determined, true = internal, false = external
    /// </summary>
    public bool? IsInternal { get; set; }

    /// <summary>
    /// Trust level of the speaker's identity
    /// </summary>
    public TrustLevel? TrustLevel { get; set; }

    /// <summary>
    /// Sources where this speaker appeared
    /// </summary>
    public virtual ICollection<Source> Sources { get; set; } = new List<Source>();
}
