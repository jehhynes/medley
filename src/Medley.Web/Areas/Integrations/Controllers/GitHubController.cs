using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Integrations.Controllers;

/// <summary>
/// Controller for GitHub integration management
/// </summary>
[Area("Integrations")]
[Authorize(Roles = "Admin")]
public class GitHubController : Controller
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<GitHubController> _logger;

    public GitHubController(IIntegrationService integrationService, ILogger<GitHubController> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the GitHub integration edit form (create or edit)
    /// </summary>
    public async Task<IActionResult> Edit(Guid? id, GitHubViewModel model)
    {
        if (Request.Method == "GET")
            ModelState.ClearValidationState(string.Empty);

        if (id.HasValue)
        {
            // Edit mode
            var integration = await _integrationService.GetByIdAsync(id.Value);
            if (integration == null)
                throw new Exception("GitHub integration not found.");

            if (integration.Type != IntegrationType.GitHub)
                throw new Exception("Integration is not a GitHub integration.");

            model.Id = integration.Id;
            model.Name = integration.Name;
            if (!string.IsNullOrWhiteSpace(model.ApiKey))
                model.ApiKey = integration.ApiKey ?? "";
            model.BaseUrl = integration.BaseUrl ?? "https://api.github.com";
        }

        return View(model);
    }

    /// <summary>
    /// Processes the GitHub integration edit form submission (create or edit)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GitHubViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Integration? integration;

        if (model.Id != null)
        {
            // Edit mode
            integration = await _integrationService.GetByIdAsync(model.Id.Value);
            if (integration == null)
                throw new Exception("GitHub integration not found.");

            if (integration.Type != IntegrationType.GitHub)
                throw new Exception("Integration is not a GitHub integration.");

            // Update existing integration
            integration.Name = model.Name;
            integration.ApiKey = model.ApiKey;
            integration.BaseUrl = model.BaseUrl;
            integration.LastModifiedAt = DateTimeOffset.UtcNow;

            TempData["Success"] = $"GitHub integration '{model.Name}' updated successfully.";
            // Entity is already tracked, changes will be saved on SaveChangesAsync
        }
        else
        {
            // Create mode
            integration = new Integration
            {
                Id = Guid.NewGuid(),
                Type = IntegrationType.GitHub,
                Name = model.Name,
                ApiKey = model.ApiKey,
                BaseUrl = model.BaseUrl,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            TempData["Success"] = $"GitHub integration '{model.Name}' created successfully.";
            await _integrationService.AddAsync(integration);
        }
        return RedirectToAction("Index", "Manage");
    }
}
