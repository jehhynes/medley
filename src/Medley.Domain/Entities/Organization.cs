using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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
    /// Indicates whether article sync to Zendesk Help Center is enabled
    /// </summary>
    public bool EnableArticleZendeskSync { get; set; } = false;

    /// <summary>
    /// IANA timezone identifier for the organization (e.g., "America/New_York", "America/Chicago", "America/Los_Angeles")
    /// Used for converting UTC timestamps to local time for reporting and analytics
    /// </summary>
    [MaxLength(50)]
    public string TimeZone { get; set; } = "America/New_York"; // Default to EST

    /// <summary>
    /// Number of approvals required before an article can be approved
    /// </summary>
    public int RequiredReviewCount { get; set; } = 1;

    /// <summary>
    /// User who must always review articles (nullable - no required reviewer if null)
    /// </summary>
    public Guid? RequiredReviewerId { get; set; }

    /// <summary>
    /// Automatically approve articles when review count is reached and required reviewer (if set) has approved
    /// </summary>
    public bool AutoApprove { get; set; } = false;

    /// <summary>
    /// Navigation property to the required reviewer user
    /// </summary>
    [ForeignKey(nameof(RequiredReviewerId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public User? RequiredReviewer { get; set; }
}


