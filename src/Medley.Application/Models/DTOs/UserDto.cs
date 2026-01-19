namespace Medley.Application.Models.DTOs;

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

/// <summary>
/// Lightweight user reference (for nested references that don't need avatar info)
/// </summary>
public class UserRef
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public required string FullName { get; set; }
}
