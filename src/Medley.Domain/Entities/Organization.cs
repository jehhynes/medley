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
    /// Company context information for the organization
    /// </summary>
    [MaxLength(2000)]
    public string? CompanyContext { get; set; }
}


