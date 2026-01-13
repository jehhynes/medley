using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class SettingsController : Controller
{
    private readonly IRepository<Template> _templateRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SettingsController> _logger;

    public SettingsController(
        IRepository<Template> templateRepository,
        IRepository<Organization> organizationRepository,
        IUnitOfWork unitOfWork,
        ILogger<SettingsController> logger)
    {
        _templateRepository = templateRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Load organization settings
        var organization = await _organizationRepository.Query()
            .FirstOrDefaultAsync();

        if (organization == null)
        {
            // Create default organization if it doesn't exist
            organization = new Organization
            {
                Name = "Default Organization",
                EmailDomain = null,
                EnableSmartTagging = false
            };
            await _organizationRepository.AddAsync(organization);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Created default organization");
        }

        ViewBag.OrganizationId = organization.Id;
        ViewBag.EmailDomain = organization.EmailDomain ?? "";
        ViewBag.EnableSmartTagging = organization.EnableSmartTagging;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOrganizationSettings(Guid organizationId, string? emailDomain, bool enableSmartTagging)
    {
        try
        {
            var organization = await _organizationRepository.GetByIdAsync(organizationId);
            if (organization == null)
            {
                TempData["ErrorMessage"] = "Organization not found";
                return RedirectToAction(nameof(Index));
            }

            // Normalize email domain (remove leading @ if present, trim whitespace)
            var normalizedDomain = string.IsNullOrWhiteSpace(emailDomain)
                ? null
                : emailDomain.Trim().TrimStart('@');

            organization.EmailDomain = normalizedDomain;
            organization.EnableSmartTagging = enableSmartTagging;
            
            await _organizationRepository.AddAsync(organization);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated organization settings for {OrganizationId}", organizationId);
            TempData["SuccessMessage"] = "Organization settings saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save organization settings");
            TempData["ErrorMessage"] = "Failed to save organization settings. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

}
