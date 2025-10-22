using Medley.Application.Integrations.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;
using Medley.Web.Areas.Integrations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Medley.Web.Areas.Integrations.Controllers;

/// <summary>
/// Main controller for integration management operations
/// </summary>
[Area("Integrations")]
[Authorize(Roles = "Admin")]
public class ManageController : Controller
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<ManageController> _logger;

    public ManageController(IIntegrationService integrationService, ILogger<ManageController> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the list of all integrations with search and filtering
    /// </summary>
    public async Task<IActionResult> Index(string? search, IntegrationType? type, ConnectionStatus? status)
    {
        try
        {
            var query = _integrationService.Query();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i => i.DisplayName!.Contains(search));
            }

            // Apply type filter
            if (type.HasValue && type.Value != IntegrationType.Unknown)
            {
                query = query.Where(i => i.Type == type.Value);
            }

            // Apply status filter (this would require a status field in the entity)
            // For now, we'll filter based on configuration presence
            if (status.HasValue)
            {
                switch (status.Value)
                {
                    case ConnectionStatus.Connected:
                        query = query.Where(i => !string.IsNullOrEmpty(i.ConfigurationJson));
                        break;
                    case ConnectionStatus.Disconnected:
                        query = query.Where(i => string.IsNullOrEmpty(i.ConfigurationJson));
                        break;
                }
            }

            var integrations = await Task.FromResult(query.ToList());

            var model = new ManageViewModel
            {
                Integrations = integrations,
                SearchTerm = search,
                SelectedType = type,
                SelectedStatus = status,
                IntegrationTypes = GetIntegrationTypeSelectList(),
                ConnectionStatuses = GetConnectionStatusSelectList()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading integrations index");
            TempData["Error"] = "An error occurred while loading integrations.";
            return View(new ManageViewModel
            {
                Integrations = new List<Integration>(),
                IntegrationTypes = GetIntegrationTypeSelectList(),
                ConnectionStatuses = GetConnectionStatusSelectList()
            });
        }
    }

    /// <summary>
    /// Displays the add integration page with type selection
    /// </summary>
    public IActionResult Add()
    {
        var model = new AddIntegrationViewModel
        {
            IntegrationTypes = GetIntegrationTypeSelectList()
        };
        return View(model);
    }

    /// <summary>
    /// Redirects to the appropriate integration type controller for creation
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Add(AddIntegrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.IntegrationTypes = GetIntegrationTypeSelectList();
            return View(model);
        }

        // Redirect to the appropriate integration type controller
        return model.SelectedType switch
        {
            IntegrationType.GitHub => RedirectToAction("Edit", "GitHub", new { area = "Integrations" }),
            IntegrationType.Slack => RedirectToAction("Edit", "Slack", new { area = "Integrations" }),
            IntegrationType.Fellow => RedirectToAction("Edit", "Fellow", new { area = "Integrations" }),
            IntegrationType.Jira => RedirectToAction("Edit", "Jira", new { area = "Integrations" }),
            IntegrationType.Zendesk => RedirectToAction("Edit", "Zendesk", new { area = "Integrations" }),
            _ => RedirectToAction(nameof(Add))
        };
    }

    /// <summary>
    /// Redirects to the appropriate integration type controller for editing
    /// </summary>
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var integration = await _integrationService.GetByIdAsync(id);
            if (integration == null)
            {
                TempData["Error"] = "Integration not found.";
                return RedirectToAction(nameof(Index));
            }

            // Redirect to the appropriate integration type controller
            return integration.Type switch
            {
                IntegrationType.GitHub => RedirectToAction("Edit", "GitHub", new { id, area = "Integrations" }),
                IntegrationType.Slack => RedirectToAction("Edit", "Slack", new { id, area = "Integrations" }),
                IntegrationType.Fellow => RedirectToAction("Edit", "Fellow", new { id, area = "Integrations" }),
                IntegrationType.Jira => RedirectToAction("Edit", "Jira", new { id, area = "Integrations" }),
                IntegrationType.Zendesk => RedirectToAction("Edit", "Zendesk", new { id, area = "Integrations" }),
                _ => RedirectToAction(nameof(Index))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading integration {IntegrationId} for edit", id);
            TempData["Error"] = "An error occurred while loading the integration.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Deletes an integration
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var integration = await _integrationService.GetByIdAsync(id);
            if (integration == null)
            {
                TempData["Error"] = "Integration not found.";
                return RedirectToAction(nameof(Index));
            }

            await _integrationService.DeleteAsync(integration);
            TempData["Success"] = $"Integration '{integration.DisplayName}' deleted successfully.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting integration {IntegrationId}", id);
            TempData["Error"] = "An error occurred while deleting the integration.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Tests the connection for an integration
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> TestConnection(Guid id)
    {
        try
        {
            var integration = await _integrationService.GetByIdAsync(id);
            if (integration == null)
            {
                return Json(new { success = false, error = "Integration not found." });
            }

            var isConnected = await _integrationService.TestConnectionAsync(integration);
            var status = await _integrationService.GetConnectionStatusAsync(integration);

            return Json(new { success = true, connected = isConnected, status = status.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for integration {IntegrationId}", id);
            return Json(new { success = false, error = "An error occurred while testing the connection." });
        }
    }

    private SelectList GetIntegrationTypeSelectList()
    {
        var types = Enum.GetValues<IntegrationType>()
            .Where(t => t != IntegrationType.Unknown)
            .Select(t => new { Value = (int)t, Text = t.ToString() })
            .ToList();

        return new SelectList(types, "Value", "Text");
    }

    private SelectList GetConnectionStatusSelectList()
    {
        var statuses = Enum.GetValues<ConnectionStatus>()
            .Select(s => new { Value = (int)s, Text = s.ToString() })
            .ToList();

        return new SelectList(statuses, "Value", "Text");
    }
}
