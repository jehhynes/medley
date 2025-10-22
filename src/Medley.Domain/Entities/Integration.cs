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

    // Serialized configuration (encrypted at rest at infra layer)
    public string? ConfigurationJson { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    public ConnectionStatus Status { get; set; } = ConnectionStatus.Unknown;

    public DateTimeOffset? LastHealthCheckAt { get; set; }
}


