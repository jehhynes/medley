using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// User audit log entity for tracking authentication and authorization events
/// </summary>
[Index(nameof(Timestamp))]
[Index(nameof(UserId))]
[Index(nameof(Action))]
public class UserAuditLog : BaseEntity
{
    /// <summary>
    /// User identifier associated with the action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Action performed
    /// </summary>
    public required UserAuditAction Action { get; set; }

    /// <summary>
    /// Timestamp when the action occurred
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Additional details about the action
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Username for display purposes
    /// </summary>
    [MaxLength(256)]
    public string? UserName { get; set; }

    /// <summary>
    /// Indicates if the action was successful
    /// </summary>
    public required bool Success { get; set; } = true;
}
