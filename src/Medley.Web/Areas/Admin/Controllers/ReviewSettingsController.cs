using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ReviewSettingsController : Controller
{
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ReviewSettingsController> _logger;

    public ReviewSettingsController(
        IRepository<Organization> organizationRepository,
        IUnitOfWork unitOfWork,
        UserManager<User> userManager,
        ILogger<ReviewSettingsController> logger)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Load organization settings
        var organization = await _organizationRepository.Query().SingleAsync();

        // Load active users for dropdowns and successor configuration
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        ViewBag.OrganizationId = organization.Id;
        ViewBag.RequiredReviewCount = organization.RequiredReviewCount;
        ViewBag.RequiredReviewerId = organization.RequiredReviewerId;
        ViewBag.AutoApprove = organization.AutoApprove;
        ViewBag.Users = users;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(
        Guid organizationId,
        int requiredReviewCount,
        Guid? requiredReviewerId,
        bool autoApprove,
        Dictionary<string, string>? reviewSuccessors)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "Organization not found";
                return RedirectToAction(nameof(Index));
            }

            // Article approval settings
            organization.RequiredReviewCount = Math.Max(1, requiredReviewCount);
            organization.RequiredReviewerId = requiredReviewerId;
            organization.AutoApprove = autoApprove;
            
            await _unitOfWork.SaveChangesAsync();

            // Update review successors for all users
            if (reviewSuccessors != null)
            {
                foreach (var kvp in reviewSuccessors)
                {
                    if (Guid.TryParse(kvp.Key, out var userId))
                    {
                        var user = await _userManager.FindByIdAsync(userId.ToString());
                        if (user != null)
                        {
                            // Parse successor ID (empty string means null)
                            Guid? successorId = null;
                            if (!string.IsNullOrEmpty(kvp.Value) && Guid.TryParse(kvp.Value, out var parsedSuccessorId))
                            {
                                // Validate successor is not self
                                if (parsedSuccessorId != userId)
                                {
                                    successorId = parsedSuccessorId;
                                }
                            }

                            user.ReviewSuccessorId = successorId;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }
            }

            _logger.LogInformation("Updated article review settings for {OrganizationId}", organizationId);
            TempData["SuccessMessage"] = "Article review settings saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save article review settings");
            TempData["ErrorMessage"] = "Failed to save settings. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }
}
