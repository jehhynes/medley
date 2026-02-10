using System.ComponentModel.DataAnnotations;

namespace Medley.Application.Configuration;

/// <summary>
/// Configuration settings for Zendesk Help Center integration
/// </summary>
public class ZendeskSettings
{
    /// <summary>
    /// Zendesk subdomain (e.g., "your-company" for your-company.zendesk.com)
    /// </summary>
    [Required]
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the Zendesk user account used for API authentication
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Zendesk API token for authentication
    /// </summary>
    [Required]
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Zendesk section ID where articles will be created
    /// </summary>
    [Required]
    public long SectionId { get; set; }

    /// <summary>
    /// Zendesk permission group ID for article access control
    /// </summary>
    [Required]
    public long PermissionGroupId { get; set; }

    /// <summary>
    /// Zendesk user segment ID for article visibility
    /// </summary>
    [Required]
    public long UserSegmentId { get; set; }

    /// <summary>
    /// Locale for article creation (default: en-us)
    /// </summary>
    public string Locale { get; set; } = "en-us";
}
