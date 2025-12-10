using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class TagTypesController : Controller
{
    private readonly IRepository<TagType> _tagTypeRepository;
    private readonly IRepository<TagOption> _tagOptionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TagTypesController> _logger;

    public TagTypesController(
        IRepository<TagType> tagTypeRepository,
        IRepository<TagOption> tagOptionRepository,
        IUnitOfWork unitOfWork,
        ILogger<TagTypesController> logger)
    {
        _tagTypeRepository = tagTypeRepository;
        _tagOptionRepository = tagOptionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var tagTypes = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(tagTypes);
    }

    public IActionResult Create()
    {
        return View(new TagType { Name = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TagType model, string? allowedValues)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _tagTypeRepository.SaveAsync(model);

        // Handle allowed values (one per line)
        var values = ParseAllowedValues(allowedValues);
        foreach (var value in values)
        {
            var option = new TagOption
            {
                TagType = model,
                TagTypeId = model.Id,
                Value = value
            };
            await _tagOptionRepository.SaveAsync(option);
        }

        await _unitOfWork.SaveChangesAsync();
        TempData["SuccessMessage"] = "Tag type created";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var tagType = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tagType == null)
        {
            return NotFound();
        }

        ViewBag.AllowedValues = string.Join(Environment.NewLine, tagType.AllowedValues.Select(v => v.Value));
        return View(tagType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, TagType model, string? allowedValues)
    {
        var existing = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (existing == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewBag.AllowedValues = allowedValues ?? "";
            return View(model);
        }

        existing.Name = model.Name;
        existing.Prompt = model.Prompt;
        existing.IsConstrained = model.IsConstrained;

        // Sync allowed values (replace)
        var newValues = ParseAllowedValues(allowedValues).ToList();
        var toRemove = existing.AllowedValues.Where(av => !newValues.Contains(av.Value, StringComparer.OrdinalIgnoreCase)).ToList();
        foreach (var remove in toRemove)
        {
            await _tagOptionRepository.DeleteAsync(remove);
        }

        foreach (var value in newValues)
        {
            if (!existing.AllowedValues.Any(av => av.Value.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                var option = new TagOption
                {
                    TagType = existing,
                    TagTypeId = existing.Id,
                    Value = value
                };
                await _tagOptionRepository.SaveAsync(option);
            }
        }

        await _tagTypeRepository.SaveAsync(existing);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tag type updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var tagType = await _tagTypeRepository.GetByIdAsync(id);
        if (tagType == null)
        {
            return NotFound();
        }

        await _tagTypeRepository.DeleteAsync(tagType);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tag type deleted";
        return RedirectToAction(nameof(Index));
    }

    private static IEnumerable<string> ParseAllowedValues(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Enumerable.Empty<string>();

        return raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}

