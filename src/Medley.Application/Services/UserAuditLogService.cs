using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;

namespace Medley.Application.Services;

/// <summary>
/// Service for user audit logging operations
/// </summary>
public class UserAuditLogService : IUserAuditLogService
{
    private readonly IRepository<UserAuditLog> _auditLogRepository;

    public UserAuditLogService(IRepository<UserAuditLog> auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogLoginAsync(Guid userId, string userName, string ipAddress)
    {
        var auditLog = new UserAuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = UserAuditAction.Login,
            IpAddress = ipAddress,
            Details = "User logged in successfully",
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.SaveAsync(auditLog);
    }

    public async Task LogFailedLoginAsync(string userName, string ipAddress, string reason)
    {
        var auditLog = new UserAuditLog
        {
            UserName = userName,
            Action = UserAuditAction.FailedLogin,
            IpAddress = ipAddress,
            Details = $"Login failed: {reason}",
            Success = false,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.SaveAsync(auditLog);
    }

    public async Task LogLogoutAsync(Guid userId, string userName, string ipAddress)
    {
        var auditLog = new UserAuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = UserAuditAction.Logout,
            IpAddress = ipAddress,
            Details = "User logged out",
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.SaveAsync(auditLog);
    }

    public async Task LogRoleChangeAsync(Guid userId, string userName, string performedBy, string details, string ipAddress)
    {
        var auditLog = new UserAuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = UserAuditAction.RoleChange,
            IpAddress = ipAddress,
            Details = $"Role changed by {performedBy}: {details}",
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.SaveAsync(auditLog);
    }

    public async Task LogUserManagementAsync(UserAuditAction action, Guid userId, string userName, string performedBy, string ipAddress, string? details = null)
    {
        var auditLog = new UserAuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            IpAddress = ipAddress,
            Details = $"Action performed by {performedBy}" + (details != null ? $": {details}" : string.Empty),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _auditLogRepository.SaveAsync(auditLog);
    }

    public IQueryable<UserAuditLog> GetUserAuditLogs()
    {
        return _auditLogRepository.Query();
    }
}
