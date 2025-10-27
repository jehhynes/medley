using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Integrations.Controllers;

/// <summary>
/// Controller for Fellow.ai integration management
/// </summary>
[Area("Integrations")]
[Authorize(Roles = "Admin")]
public class FellowController : Controller
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<FellowController> _logger;

    public FellowController(IIntegrationService integrationService, ILogger<FellowController> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the Fellow integration edit form (create or edit)
    /// </summary>
    public async Task<IActionResult> Edit(Guid? id, FellowViewModel model)
    {
        if (Request.Method == "GET")
            ModelState.ClearValidationState(string.Empty);

        if (model.Id.HasValue)
        {
            // Edit mode
            var integration = await _integrationService.GetByIdAsync(model.Id.Value);
                
            if (integration == null)
                throw new Exception( "Fellow integration not found.");

            if (integration.Type != IntegrationType.Fellow)
                throw new Exception( "Integration is not a Fellow integration.");

            model.Id = integration.Id;
            model.DisplayName = integration.DisplayName ?? "";
            model.ApiKey = integration.ApiKey ?? "";
            model.BaseUrl = integration.BaseUrl ?? "https://mycompany.fellow.app";
        }

        return View(model);
    }

    /// <summary>
    /// Processes the Fellow integration edit form submission (create or edit)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Edit(FellowViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Integration? integration;

        if (model.IsEdit)
        {
            // Edit mode
            integration = await _integrationService.GetByIdAsync(model.Id!.Value);
            if (integration == null)
                throw new Exception("Fellow integration not found.");

            if (integration.Type != IntegrationType.Fellow)
                throw new Exception("Integration is not a Fellow integration.");

            // Update existing integration
            integration.DisplayName = model.DisplayName;
            if (!string.IsNullOrWhiteSpace(model.ApiKey))
                integration.ApiKey = model.ApiKey;
            integration.BaseUrl = model.BaseUrl;
            integration.LastModifiedAt = DateTimeOffset.UtcNow;

            TempData["Success"] = $"Fellow integration '{model.DisplayName}' updated successfully.";
        }
        else
        {
            // Create mode
            integration = new Integration
            {
                Id = Guid.NewGuid(),
                Type = IntegrationType.Fellow,
                DisplayName = model.DisplayName,
                ApiKey = model.ApiKey,
                BaseUrl = model.BaseUrl,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            TempData["Success"] = $"Fellow integration '{model.DisplayName}' created successfully.";
        }

        await _integrationService.SaveAsync(integration);
        return RedirectToAction("Index", "Manage");
    }
}
