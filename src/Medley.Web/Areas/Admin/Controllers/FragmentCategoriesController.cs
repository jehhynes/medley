using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class FragmentCategoriesController : Controller
{
    private readonly IRepository<FragmentCategory> _fragmentCategoryRepository;
    private readonly IRepository<Fragment> _fragmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FragmentCategoriesController> _logger;

    public FragmentCategoriesController(
        IRepository<FragmentCategory> fragmentCategoryRepository,
        IRepository<Fragment> fragmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<FragmentCategoriesController> logger)
    {
        _fragmentCategoryRepository = fragmentCategoryRepository;
        _fragmentRepository = fragmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var fragmentCategories = await _fragmentCategoryRepository.Query()
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(fragmentCategories);
    }

    public IActionResult Create()
    {
        return View(new FragmentCategory { Name = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FragmentCategory model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _fragmentCategoryRepository.AddAsync(model);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Fragment category created";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_fragment_categories_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "A fragment category with this name already exists.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var fragmentCategory = await _fragmentCategoryRepository.GetByIdAsync(id);

        if (fragmentCategory == null)
        {
            return NotFound();
        }

        return View(fragmentCategory);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FragmentCategory model)
    {
        var existing = await _fragmentCategoryRepository.GetByIdAsync(id);

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

            TempData["SuccessMessage"] = "Fragment category updated";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_fragment_categories_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "A fragment category with this name already exists.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var fragmentCategory = await _fragmentCategoryRepository.GetByIdAsync(id);
        if (fragmentCategory == null)
        {
            return NotFound();
        }

        // Check if any fragments use this category
        var fragmentCount = await _fragmentRepository.Query()
            .CountAsync(f => f.FragmentCategoryId == id);

        if (fragmentCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete this category because {fragmentCount} fragment(s) are using it. Please reassign those fragments first.";
            return RedirectToAction(nameof(Index));
        }

        await _fragmentCategoryRepository.DeleteAsync(fragmentCategory);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Fragment category deleted";
        return RedirectToAction(nameof(Index));
    }
}
