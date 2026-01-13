using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class ArticleTypesController : Controller
{
    private readonly IRepository<ArticleType> _articleTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleTypesController> _logger;

    public ArticleTypesController(
        IRepository<ArticleType> articleTypeRepository,
        IUnitOfWork unitOfWork,
        ILogger<ArticleTypesController> logger)
    {
        _articleTypeRepository = articleTypeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(t => t.Name)
            .ToListAsync();

        return View(articleTypes);
    }

    public IActionResult Create()
    {
        return View(new ArticleType { Name = string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleType model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _articleTypeRepository.AddAsync(model);
        await _unitOfWork.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Article type created";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var articleType = await _articleTypeRepository.GetByIdAsync(id);

        if (articleType == null)
        {
            return NotFound();
        }

        return View(articleType);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ArticleType model)
    {
        var existing = await _articleTypeRepository.GetByIdAsync(id);

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

        await _articleTypeRepository.AddAsync(existing);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Article type updated";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var articleType = await _articleTypeRepository.GetByIdAsync(id);
        if (articleType == null)
        {
            return NotFound();
        }

        await _articleTypeRepository.DeleteAsync(articleType);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Article type deleted";
        return RedirectToAction(nameof(Index));
    }
}

