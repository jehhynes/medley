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

        var user = new User
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} created successfully", model.Email);

            // Generate email confirmation token and send email
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                nameof(ConfirmEmail),
                "Auth",
                new { userId = user.Id, code },
                protocol: Request.Scheme);

            try
            {
                var emailBody = EmailTemplateService.GetEmailConfirmationTemplate(callbackUrl!);
                await _emailService.SendEmailAsync(model.Email, "Confirm your email - Medley", emailBody);
                TempData["Success"] = "Registration successful! Please check your email to confirm your account.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", model.Email);
                TempData["Warning"] = "Account created but confirmation email failed to send. Please contact support.";
            }

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

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditLogService.LogLoginAsync(user.Id, user.UserName!, ipAddress);
            _logger.LogInformation("User {Email} logged in", model.Email);

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
