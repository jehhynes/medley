using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
    public async Task<IActionResult> Edit(Guid? id = null)
    {
        try
        {
            var model = new GitHubViewModel();

            if (id.HasValue)
            {
                // Edit mode
                var integration = await _integrationService.GetByIdAsync(id.Value);
                if (integration == null)
                {
                    TempData["Error"] = "GitHub integration not found.";
                    return RedirectToAction("Index", "Manage");
                }

                if (integration.Type != IntegrationType.GitHub)
                {
                    TempData["Error"] = "Integration is not a GitHub integration.";
                    return RedirectToAction("Index", "Manage");
                }

                var config = ParseConfiguration(integration.ConfigurationJson);
                model.Id = integration.Id;
                model.Form.DisplayName = integration.DisplayName ?? "";
                model.Form.ApiKey = config?.GetValueOrDefault("apiKey", "") ?? "";
                model.Form.BaseUrl = config?.GetValueOrDefault("baseUrl", "https://api.github.com") ?? "https://api.github.com";
            }

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading GitHub integration {IntegrationId} for form", id);
            TempData["Error"] = "An error occurred while loading the GitHub integration.";
            return RedirectToAction("Index", "Manage");
        }
    }

    /// <summary>
    /// Processes the GitHub integration edit form submission (create or edit)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GitHubViewModel model)
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
                    TempData["Error"] = "GitHub integration not found.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                if (integration.Type != IntegrationType.GitHub)
                {
                    TempData["Error"] = "Integration is not a GitHub integration.";
                    return RedirectToAction("Index", "Manage", new { area = "" });
                }

                // Update existing integration
                integration.DisplayName = model.Form.DisplayName;
                integration.ConfigurationJson = BuildConfigurationJson(model);
                integration.LastModifiedAt = DateTimeOffset.UtcNow;

                TempData["Success"] = $"GitHub integration '{model.Form.DisplayName}' updated successfully.";
            }
            else
            {
                // Create mode
                integration = new Integration
                {
                    Id = Guid.NewGuid(),
                    Type = IntegrationType.GitHub,
                    DisplayName = model.Form.DisplayName,
                    ConfigurationJson = BuildConfigurationJson(model),
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                TempData["Success"] = $"GitHub integration '{model.Form.DisplayName}' created successfully.";
            }

            await _integrationService.SaveAsync(integration);
            return RedirectToAction("Index", "Manage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving GitHub integration {IntegrationId}", model.Id);
            ModelState.AddModelError("", $"An error occurred while {(model.IsEdit ? "updating" : "creating")} the GitHub integration.");
            return View(model);
        }
    }

    private string BuildConfigurationJson(GitHubViewModel model)
    {
        var config = new Dictionary<string, string>
        {
            ["apiKey"] = model.Form.ApiKey ?? "",
            ["baseUrl"] = model.Form.BaseUrl ?? "https://api.github.com"
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
