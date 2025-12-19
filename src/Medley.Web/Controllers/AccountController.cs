using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

/// <summary>
/// Controller for user account management operations
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserAuditLogService _auditLogService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IUserAuditLogService auditLogService,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Display change password form
    /// </summary>
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    /// <summary>
    /// Process password change request
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("User not found when attempting to change password");
            return RedirectToAction("Login", "Auth");
        }

        // Attempt to change the password
        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            // Get IP address for audit logging
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // Log the password change event
            await _auditLogService.LogUserManagementAsync(
                UserAuditAction.PasswordChanged,
                user.Id,
                user.UserName!,
                user.UserName!,
                ipAddress,
                "User changed their own password");

            // Sign the user in again to refresh the security stamp
            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("User {UserId} successfully changed their password", user.Id);
            TempData["Success"] = "Your password has been changed successfully.";

            return RedirectToAction("ChangePassword");
        }

        // If we got here, the password change failed
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        _logger.LogWarning("Failed password change attempt for user {UserId}", user.Id);
        return View(model);
    }
}

