using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Custom user entity extending ASP.NET Core Identity
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// User's full name
    /// </summary>
    public required string FullName { get; set; }

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

    /// <summary>
    /// User to automatically reassign articles to after this user submits a review
    /// </summary>
    public Guid? ReviewSuccessorId { get; set; }

    /// <summary>
    /// Navigation property to the review successor user
    /// </summary>
    [ForeignKey(nameof(ReviewSuccessorId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public User? ReviewSuccessor { get; set; }
}
