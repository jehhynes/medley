using Microsoft.AspNetCore.Identity;

namespace Medley.Domain.Entities;

/// <summary>
/// Custom user entity extending ASP.NET Core Identity
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// User's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Date when the user was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Date when the user was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User's initials for avatar display
    /// </summary>
    public string? Initials { get; set; }

    /// <summary>
    /// User's avatar color (hex format)
    /// </summary>
    public string? Color { get; set; }
}
