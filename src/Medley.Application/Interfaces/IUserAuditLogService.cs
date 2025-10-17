using Medley.Domain.Entities;
using Medley.Domain.Enums;

namespace Medley.Application.Interfaces;

/// <summary>
/// Service interface for user audit logging operations
/// </summary>
public interface IUserAuditLogService
{
    /// <summary>
    /// Logs a successful login event
    /// </summary>
    Task LogLoginAsync(Guid userId, string userName, string ipAddress);

    /// <summary>
    /// Logs a failed login attempt
    /// </summary>
    Task LogFailedLoginAsync(string userName, string ipAddress, string reason);

    /// <summary>
    /// Logs a logout event
    /// </summary>
    Task LogLogoutAsync(Guid userId, string userName, string ipAddress);

    /// <summary>
    /// Logs a role change event
    /// </summary>
    Task LogRoleChangeAsync(Guid userId, string userName, string performedBy, string details, string ipAddress);

    /// <summary>
    /// Logs a user management action
    /// </summary>
    Task LogUserManagementAsync(UserAuditAction action, Guid userId, string userName, string performedBy, string ipAddress, string? details = null);

    /// <summary>
    /// Gets user audit logs with optional filtering
    /// </summary>
    IQueryable<UserAuditLog> GetUserAuditLogs();
}
