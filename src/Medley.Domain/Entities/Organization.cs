using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a tenant organization for multi-tenant data isolation
/// </summary>
public class Organization : BaseEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Email domain for the organization (e.g., "company.com")
    /// Used to identify internal vs external attendees
    /// </summary>
    [MaxLength(100)]
    public string? EmailDomain { get; set; }

    /// <summary>
    /// Indicates whether smart tagging (automatic IsInternal detection) is enabled
    /// </summary>
    public bool EnableSmartTagging { get; set; } = false;

    /// <summary>
    /// Indicates whether speaker extraction from meeting transcripts is enabled
    /// </summary>
    public bool EnableSpeakerExtraction { get; set; } = false;

    /// <summary>
    /// IANA timezone identifier for the organization (e.g., "America/New_York", "America/Chicago", "America/Los_Angeles")
    /// Used for converting UTC timestamps to local time for reporting and analytics
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "America/New_York"; // Default to EST
}


