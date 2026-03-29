using Medley.Application.Integrations.Interfaces;
using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

/// <summary>
/// Controller for importing YouTube videos via SocialKit
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class YouTubeImportController : Controller
{
    private readonly IYouTubeImportService _importService;
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<YouTubeImportController> _logger;

    public YouTubeImportController(
        IYouTubeImportService importService,
        IIntegrationService integrationService,
        ILogger<YouTubeImportController> logger)
    {
        _importService = importService;
        _integrationService = integrationService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View(new YouTubeImportViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(YouTubeImportViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.UrlsText))
        {
            ModelState.AddModelError("", "Please enter at least one YouTube URL.");
            return View("Index", model);
        }

        var urls = model.UrlsText
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();

        if (urls.Count == 0)
        {
            ModelState.AddModelError("", "No valid URLs found.");
            return View("Index", model);
        }

        try
        {
            _logger.LogInformation("Starting YouTube import for {Count} URL(s)", urls.Count);

            var integration = await GetOrCreateManualIntegrationAsync();
            var result = await _importService.ImportAsync(urls, integration);

            model.ImportResult = result;

            if (result.Success)
            {
                TempData["Success"] = $"Successfully imported {result.SourcesImported} video(s)!";
            }
            else
            {
                TempData["Error"] = "Import completed with errors. Please review the details below.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during YouTube import");
            ModelState.AddModelError("", $"An error occurred: {ex.Message}");
        }

        return View("Index", model);
    }

    private async Task<Integration> GetOrCreateManualIntegrationAsync()
    {
        var existing = await _integrationService.Query()
            .FirstOrDefaultAsync(i => i.Type == IntegrationType.Manual);

        if (existing is not null)
            return existing;

        var integration = new Integration
        {
            Type = IntegrationType.Manual,
            Name = "Manual Import",
            Status = ConnectionStatus.Connected
        };

        await _integrationService.AddAsync(integration);
        _logger.LogInformation("Created Manual integration: {IntegrationId}", integration.Id);

        return integration;
    }
}
