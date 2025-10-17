namespace Medley.Domain.Enums;

/// <summary>
/// Enumeration of user audit log action types
/// </summary>
public enum UserAuditAction
{
    Login,
    Logout,
    FailedLogin,
    RoleChange,
    UserCreated,
    UserUpdated,
    UserDeleted,
    PasswordChanged,
    EmailConfirmed,
    AccountLocked,
    AccountUnlocked
}
