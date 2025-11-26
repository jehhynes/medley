using Medley.Application.Interfaces;
using Medley.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Admin.Controllers;

/// <summary>
/// Controller for importing Knowledge Builder articles
/// </summary>
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class KnowledgeBuilderImportController : Controller
{
    private readonly IKnowledgeBuilderImportService _importService;
    private readonly ILogger<KnowledgeBuilderImportController> _logger;
    private const int MaxFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    public KnowledgeBuilderImportController(
        IKnowledgeBuilderImportService importService,
        ILogger<KnowledgeBuilderImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Display the Knowledge Builder import form
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        var model = new KnowledgeBuilderImportViewModel
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
    public async Task<IActionResult> Upload(IFormFile file)
    {
        var model = new KnowledgeBuilderImportViewModel
        {
            AllowedFileTypes = new[] { ".json", ".zip" },
            MaxFileSizeMB = MaxFileSizeBytes / (1024 * 1024)
        };

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Please select a file to upload.");
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
            _logger.LogInformation("Processing Knowledge Builder import file: {FileName} ({Size} bytes)", 
                file.FileName, file.Length);

            using var stream = file.OpenReadStream();
            var result = await _importService.ProcessFileAsync(stream, file.FileName);

            model.ImportResult = result;

            if (result.Success)
            {
                TempData["Success"] = $"Successfully imported {result.ArticlesImported} articles!";
            }
            else
            {
                TempData["Error"] = "Import completed with errors. Please review the details below.";
            }

            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Knowledge Builder import file: {FileName}", file.FileName);
            ModelState.AddModelError("", $"An error occurred while processing the file: {ex.Message}");
            return View("Index", model);
        }
    }
}

