using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/users")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class UsersApiController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UsersApiController> _logger;

    public UsersApiController(
        UserManager<User> userManager,
        ILogger<UsersApiController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all active users for assignment
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<UserSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<UserSummaryDto>>> GetAll()
    {
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Initials = u.Initials,
                Color = u.Color
            })
            .ToListAsync();

        return Ok(users);
    }
}
