using Medley.Domain.Enums;

namespace Medley.Web.Models;

/// <summary>
/// View model for displaying user audit logs
/// </summary>
public class UserAuditLogViewModel
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public UserAuditAction Action { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public bool Success { get; set; }
}
