using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    public async Task<IActionResult> Edit(Guid? id = null)
    {
        try
        {
            var model = new FellowViewModel();

            if (id.HasValue)
            {
                // Edit mode
                var integration = await _integrationService.GetByIdAsync(id.Value);
                if (integration == null)
                {
                    TempData["Error"] = "Fellow integration not found.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                if (integration.Type != IntegrationType.Fellow)
                {
                    TempData["Error"] = "Integration is not a Fellow integration.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                var config = ParseConfiguration(integration.ConfigurationJson);
                model.Id = integration.Id;
                model.Form.DisplayName = integration.DisplayName ?? "";
                model.Form.ApiKey = config?.GetValueOrDefault("apiKey", "") ?? "";
                model.Form.BaseUrl = config?.GetValueOrDefault("baseUrl", "https://api.fellow.app") ?? "https://api.fellow.app";
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Fellow integration {IntegrationId} for form", id);
            TempData["Error"] = "An error occurred while loading the Fellow integration.";
            return RedirectToAction("Index", "Manage");
        }
    }

    /// <summary>
    /// Processes the Fellow integration edit form submission (create or edit)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FellowViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Integration integration;

            if (model.IsEdit)
            {
                // Edit mode
                integration = await _integrationService.GetByIdAsync(model.Id!.Value);
                if (integration == null)
                {
                    TempData["Error"] = "Fellow integration not found.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                if (integration.Type != IntegrationType.Fellow)
                {
                    TempData["Error"] = "Integration is not a Fellow integration.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                // Update existing integration
                integration.DisplayName = model.Form.DisplayName;
                integration.ConfigurationJson = BuildConfigurationJson(model);
                integration.LastModifiedAt = DateTimeOffset.UtcNow;

                TempData["Success"] = $"Fellow integration '{model.Form.DisplayName}' updated successfully.";
            }
            else
            {
                // Create mode
                integration = new Integration
                {
                    Id = Guid.NewGuid(),
                    Type = IntegrationType.Fellow,
                    DisplayName = model.Form.DisplayName,
                    ConfigurationJson = BuildConfigurationJson(model),
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                TempData["Success"] = $"Fellow integration '{model.Form.DisplayName}' created successfully.";
            }

            await _integrationService.SaveAsync(integration);
            return RedirectToAction("Index", "Manage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving Fellow integration {IntegrationId}", model.Id);
            ModelState.AddModelError("", $"An error occurred while {(model.IsEdit ? "updating" : "creating")} the Fellow integration.");
            return View(model);
        }
    }

    private string BuildConfigurationJson(FellowViewModel model)
    {
        var config = new Dictionary<string, string>
        {
            ["apiKey"] = model.Form.ApiKey ?? "",
            ["baseUrl"] = model.Form.BaseUrl ?? "https://api.fellow.app"
        };

        return JsonSerializer.Serialize(config);
    }

    private Dictionary<string, string>? ParseConfiguration(string? configurationJson)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(configurationJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
