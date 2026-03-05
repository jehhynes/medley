using Medley.Application.Interfaces;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
public class AuthController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IUserAuditLogService auditLogService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Display registration form
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Process user registration
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Only allow registration if no users exist (initial setup only)
        var existingUsersCount = _userManager.Users.Count();
        if (existingUsersCount > 0)
        {
            _logger.LogWarning("Registration attempt blocked - system already has users");
            throw new InvalidOperationException("Registration is only available during initial setup. The system already has registered users.");
        }

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            EmailConfirmed = true, // Automatically confirm email for initial setup
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Add Admin role to the first user
            var roleResult = await _userManager.AddToRoleAsync(user, "Admin");
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Failed to add Admin role to user {Email}: {Errors}", 
                    model.Email, 
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
            else
            {
                _logger.LogInformation("Admin role assigned to user {Email}", model.Email);
            }

            _logger.LogInformation("User {Email} created successfully during initial setup", model.Email);
            TempData["Success"] = "Registration successful! You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    /// <summary>
    /// Display login form
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Redirect to registration if no users exist (initial setup)
        var existingUsersCount = _userManager.Users.Count();
        if (existingUsersCount == 0)
        {
            _logger.LogInformation("No users exist - redirecting to registration for initial setup");
            return RedirectToAction(nameof(Register), new { returnUrl });
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Process user login
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (user == null)
        {
            await _auditLogService.LogFailedLoginAsync(model.Email, ipAddress, "User not found");
            ModelState.AddModelError(string.Empty, "Invalid login attempt");
            return View(model);
        }

        // Sign out any existing session first
        await _signInManager.SignOutAsync();

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password, 
            isPersistent: model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditLogService.LogLoginAsync(user.Id, user.UserName!, ipAddress);
            _logger.LogInformation("User {Email} logged in with RememberMe={RememberMe}", model.Email, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            await _auditLogService.LogFailedLoginAsync(model.Email, ipAddress, "Account locked out");
            _logger.LogWarning("User {Email} account locked out", model.Email);
            return View("Lockout");
        }

        await _auditLogService.LogFailedLoginAsync(model.Email, ipAddress, "Invalid password");
        ModelState.AddModelError(string.Empty, "Invalid login attempt");
        return View(model);
    }

    /// <summary>
    /// Process user logout
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        if (user != null)
        {
            await _auditLogService.LogLogoutAsync(user.Id, user.UserName!, ipAddress);
        }

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Display access denied page
    /// </summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>
    /// Confirm email address
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(Guid userId, string code)
    {
        if (userId == Guid.Empty)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} confirmed their email", user.Email);
            TempData["Success"] = "Email confirmed successfully! You can now log in.";
        }
        else
        {
            TempData["Error"] = "Error confirming email. The link may have expired.";
        }

        return RedirectToAction(nameof(Login));
    }
}
