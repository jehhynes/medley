namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for User entity
/// </summary>
public class UserDto
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// User's email address
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// User's initials for avatar display
    /// </summary>
    public string? Initials { get; set; }

    /// <summary>
    /// User's avatar color (hex format)
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Indicates if the user account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the user was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Summary information about a user (for nested references)
/// </summary>
public class UserSummaryDto
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public required string FullName { get; set; }

    /// <summary>
    /// User's initials for avatar display
    /// </summary>
    public string? Initials { get; set; }

    /// <summary>
    /// User's avatar color (hex format)
    /// </summary>
    public string? Color { get; set; }
}
