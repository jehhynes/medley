using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers;

/// <summary>
/// Controller for user management operations (Admin only)
/// </summary>
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IUserAuditLogService _auditLogService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IUserAuditLogService auditLogService,
        ILogger<UsersController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Display user list with search and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm = null)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u => 
                u.Email!.Contains(searchTerm) || 
                u.FullName!.Contains(searchTerm));
        }

        var users = await query
            .OrderBy(u => u.Email)
            .ToListAsync();

        ViewData["SearchTerm"] = searchTerm;
        return View(users);
    }

    /// <summary>
    /// Display user edit form
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email!,
            FullName = user.FullName ?? string.Empty,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            IsLockedOut = await _userManager.IsLockedOutAsync(user),
            AvailableRoles = allRoles,
            UserRoles = userRoles.ToList()
        };

        return View(model);
    }

    /// <summary>
    /// Process user edit form
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model, List<string> selectedRoles)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Update user properties
        user.IsActive = model.IsActive;
        user.EmailConfirmed = model.EmailConfirmed;
        user.LastModifiedAt = DateTimeOffset.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToAdd = selectedRoles.Except(currentRoles).ToList();
        var rolesToRemove = currentRoles.Except(selectedRoles).ToList();

        if (rolesToAdd.Any())
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd);
            await _auditLogService.LogRoleChangeAsync(
                user.Id,
                user.UserName!,
                currentUser?.UserName ?? "System",
                $"Added roles: {string.Join(", ", rolesToAdd)}",
                ipAddress);
        }

        if (rolesToRemove.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            await _auditLogService.LogRoleChangeAsync(
                user.Id,
                user.UserName!,
                currentUser?.UserName ?? "System",
                $"Removed roles: {string.Join(", ", rolesToRemove)}",
                ipAddress);
        }

        // Handle lockout
        if (model.IsLockedOut && !await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            await _auditLogService.LogUserManagementAsync(
                UserAuditAction.AccountLocked,
                user.Id,
                user.UserName!,
                currentUser?.UserName ?? "System",
                ipAddress);
        }
        else if (!model.IsLockedOut && await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _auditLogService.LogUserManagementAsync(
                UserAuditAction.AccountUnlocked,
                user.Id,
                user.UserName!,
                currentUser?.UserName ?? "System",
                ipAddress);
        }

        _logger.LogInformation("User {UserId} updated by {AdminUser}", user.Id, currentUser?.UserName);
        TempData["Success"] = "User updated successfully";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Delete user with confirmation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Id == id)
        {
            TempData["Error"] = "You cannot delete your own account";
            return RedirectToAction(nameof(Index));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            await _auditLogService.LogUserManagementAsync(
                UserAuditAction.UserDeleted,
                user.Id,
                user.UserName!,
                currentUser?.UserName ?? "System",
                ipAddress);

            _logger.LogInformation("User {UserId} deleted by {AdminUser}", user.Id, currentUser?.UserName);
            TempData["Success"] = "User deleted successfully";
        }
        else
        {
            TempData["Error"] = "Failed to delete user";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Display audit log viewer
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AuditLog(int page = 1, int pageSize = 50)
    {
        var query = _auditLogService.GetUserAuditLogs()
            .OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync();
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);
        ViewData["TotalCount"] = totalCount;

        return View(logs);
    }
}
