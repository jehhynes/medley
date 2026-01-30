using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class KnowledgeCategoriesController : Controller
{
    private readonly IRepository<KnowledgeCategory> _knowledgeCategoryRepository;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KnowledgeCategoriesController> _logger;

    public KnowledgeCategoriesController(
        IRepository<KnowledgeCategory> knowledgeCategoryRepository,
        IRepository<Fragment> fragmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<KnowledgeCategoriesController> logger)
    {
        _knowledgeCategoryRepository = knowledgeCategoryRepository;
        _fragmentRepository = fragmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var knowledgeCategories = await _knowledgeCategoryRepository.Query()
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(knowledgeCategories);
    }

    public IActionResult Create()
    {
        return View(new KnowledgeCategory { Name = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(KnowledgeCategory model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _knowledgeCategoryRepository.AddAsync(model);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Knowledge category created";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_knowledge_categories_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "A knowledge category with this name already exists.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var knowledgeCategory = await _knowledgeCategoryRepository.GetByIdAsync(id);

        if (knowledgeCategory == null)
        {
            return NotFound();
        }

        return View(knowledgeCategory);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, KnowledgeCategory model)
    {
        var existing = await _knowledgeCategoryRepository.GetByIdAsync(id);

        if (existing == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        existing.Name = model.Name;
        existing.Icon = model.Icon;

        try
        {
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Knowledge category updated";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_knowledge_categories_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "A knowledge category with this name already exists.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var knowledgeCategory = await _knowledgeCategoryRepository.GetByIdAsync(id);
        if (knowledgeCategory == null)
        {
            return NotFound();
        }

        // Check if any fragments use this category
        var fragmentCount = await _fragmentRepository.Query()
            .CountAsync(f => f.KnowledgeCategoryId == id);

        if (fragmentCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete this category because {fragmentCount} fragment(s) are using it. Please reassign those fragments first.";
            return RedirectToAction(nameof(Index));
        }

        await _knowledgeCategoryRepository.DeleteAsync(knowledgeCategory);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Knowledge category deleted";
        return RedirectToAction(nameof(Index));
    }
}
