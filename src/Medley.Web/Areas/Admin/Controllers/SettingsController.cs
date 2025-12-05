using Medley.Application.Interfaces;
using Medley.Domain.Entities;
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
        // Load FragmentExtraction template
        var template = await _templateRepository.Query()
            .FirstOrDefaultAsync(t => t.Type == "FragmentExtraction");

        if (template == null)
        {
            // Create default template if it doesn't exist
            template = new Template
            {
                Name = "Fragment Extraction Prompt",
                Type = "FragmentExtraction",
                Content = GetDefaultFragmentExtractionPrompt()
            };
            await _templateRepository.SaveAsync(template);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Created default FragmentExtraction template");
        }

        ViewBag.FragmentExtractionPrompt = template.Content;
        ViewBag.FragmentExtractionTemplateId = template.Id;

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
            await _organizationRepository.SaveAsync(organization);
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
    public async Task<IActionResult> SaveFragmentExtractionPrompt(Guid templateId, string promptContent)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(promptContent))
            {
                TempData["ErrorMessage"] = "Prompt content cannot be empty";
                return RedirectToAction(nameof(Index));
            }

            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found";
                return RedirectToAction(nameof(Index));
            }

            template.Content = promptContent.Trim();
            await _templateRepository.SaveAsync(template);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated FragmentExtraction template {TemplateId}", templateId);
            TempData["SuccessMessage"] = "Fragment extraction prompt saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save FragmentExtraction template");
            TempData["ErrorMessage"] = "Failed to save prompt. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetFragmentExtractionPrompt(Guid templateId)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(templateId);
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found";
                return RedirectToAction(nameof(Index));
            }

            template.Content = GetDefaultFragmentExtractionPrompt();
            await _templateRepository.SaveAsync(template);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Reset FragmentExtraction template {TemplateId} to default", templateId);
            TempData["SuccessMessage"] = "Fragment extraction prompt reset to default";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset FragmentExtraction template");
            TempData["ErrorMessage"] = "Failed to reset prompt. Please try again.";
        }

        return RedirectToAction(nameof(Index));
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
            
            await _organizationRepository.SaveAsync(organization);
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

    private static string GetDefaultFragmentExtractionPrompt()
    {
        return @"Analyze the following source content and extract meaningful knowledge fragments.

For each fragment you identify, provide:
- Title: A clear, descriptive title
- Summary: A brief summary (1-2 sentences)
- Category: Choose from [Decision, Action Item, Feature Request, User Feedback, Technical Discussion, Other]
- Content: The full extracted text

Source Content:
{{SOURCE_CONTENT}}";
    }
}

