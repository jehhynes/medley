using Medley.Domain.Entities;

namespace Medley.Application.Interfaces;

/// <summary>
/// Provides context information for the current request, including the authenticated user
/// </summary>
public interface IMedleyContext
{
    /// <summary>
    /// Gets the current authenticated user. Returns null if not authenticated.
    /// The user is fetched once per request and cached.
    /// </summary>
    Task<User?> GetCurrentUserAsync();

    /// <summary>
    /// Gets the current user's ID. Returns null if not authenticated.
    /// </summary>
    Guid? CurrentUserId { get; }

    /// <summary>
    /// Indicates whether the current request is from an authenticated user
    /// </summary>
    bool IsAuthenticated { get; }
}

