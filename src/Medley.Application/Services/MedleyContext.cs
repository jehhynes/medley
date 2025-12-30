using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Medley.Application.Services;

/// <summary>
/// Provides context information for the current request with lazy-loaded user caching
/// </summary>
public class MedleyContext : IMedleyContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;
    
    private User? _currentUser;
    private bool _userLoaded = false;

    public MedleyContext(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<User?> GetCurrentUserAsync()
    {
        if (!_userLoaded)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                _currentUser = await _userManager.GetUserAsync(httpContext.User);
            }
            _userLoaded = true;
        }
        
        return _currentUser;
    }

    /// <inheritdoc />
    public Guid? CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    return userId;
                }
            }
            return null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
        }
    }
}

