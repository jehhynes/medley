using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// External integration configuration for an organization (e.g., GitHub, Slack)
/// </summary>
public class Integration : BusinessEntity
{
    public required IntegrationType Type { get; set; }

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// API key or token for the integration (encrypted at rest at infra layer)
    /// </summary>
    [MaxLength(500)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Base URL for the integration API
    /// </summary>
    [MaxLength(500)]
    public string? BaseUrl { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public ConnectionStatus Status { get; set; } = ConnectionStatus.Unknown;

    public DateTimeOffset? LastHealthCheckAt { get; set; }

    /// <summary>
    /// Indicates whether the initial full sync has been completed for this integration
    /// </summary>
    public bool InitialSyncCompleted { get; set; } = false;
}


