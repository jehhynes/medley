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
/// Controller for importing Sources from Collector exports
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SourceImportController : Controller
{
    private readonly ICollectorImportService _importService;
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<SourceImportController> _logger;
    private const int MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public SourceImportController(
        ICollectorImportService importService,
        IIntegrationService integrationService,
        ILogger<SourceImportController> logger)
    {
        _importService = importService;
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Display the Source import form
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var model = new SourceImportViewModel
        {
            AllowedFileTypes = new[] { ".json", ".zip" },
            MaxFileSizeMB = MaxFileSizeBytes / (1024 * 1024)
        };

        return View(model);
    }

    /// <summary>
    /// Handle file upload and import
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSizeBytes)]
    public async Task<IActionResult> Upload(IFormFile file, SourceMetadataType metadataType)
    {
        var model = new SourceImportViewModel
        {
            AllowedFileTypes = new[] { ".json", ".zip" },
            MaxFileSizeMB = MaxFileSizeBytes / (1024 * 1024),
            SelectedMetadataType = metadataType
        };

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
            return View("Index", model);
        }

        // Validate metadata type
        if (metadataType == SourceMetadataType.Unknown)
        {
            ModelState.AddModelError("", "Please select a valid source format.");
            return View("Index", model);
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".json" && extension != ".zip")
        {
            ModelState.AddModelError("", "Only .json and .zip files are supported.");
            return View("Index", model);
        }

        // Validate file size
        if (file.Length > MaxFileSizeBytes)
        {
            ModelState.AddModelError("", $"File size exceeds the maximum limit of {model.MaxFileSizeMB} MB.");
            return View("Index", model);
        }

        try
        {
            _logger.LogInformation("Processing Source import file: {FileName} ({Size} bytes) with format {Format}", 
                file.FileName, file.Length, metadataType);

            // Get or create the Manual integration for imports
            var integration = await GetOrCreateManualIntegrationAsync();

            using var stream = file.OpenReadStream();
            var result = await _importService.ProcessFileAsync(stream, file.FileName, metadataType, integration);

            model.ImportResult = result;

            if (result.Success)
            {
                TempData["Success"] = $"Successfully imported {result.SourcesImported} sources!";
            }
            else
            {
                TempData["Error"] = "Import completed with errors. Please review the details below.";
            }

            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Source import file: {FileName}", file.FileName);
            ModelState.AddModelError("", $"An error occurred while processing the file: {ex.Message}");
            return View("Index", model);
        }
    }

    /// <summary>
    /// Gets or creates the Manual integration for imports
    /// </summary>
    private async Task<Integration> GetOrCreateManualIntegrationAsync()
    {
        // Look for existing Manual integration
        var existing = await _integrationService.Query()
            .FirstOrDefaultAsync(i => i.Type == IntegrationType.Manual);

        if (existing != null)
        {
            return existing;
        }

        // Create a new Manual integration
        var integration = new Integration
        {
            Type = IntegrationType.Manual,
            DisplayName = "Manual Import",
            Status = ConnectionStatus.Connected
        };

        await _integrationService.AddAsync(integration);
        _logger.LogInformation("Created Manual integration for imports: {IntegrationId}", integration.Id);

        return integration;
    }
}

