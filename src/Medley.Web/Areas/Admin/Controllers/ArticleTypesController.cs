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
    private readonly IRepository<Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArticleTypesController> _logger;

    public ArticleTypesController(
        IRepository<ArticleType> articleTypeRepository,
        IRepository<Article> articleRepository,
        IUnitOfWork unitOfWork,
        ILogger<ArticleTypesController> logger)
    {
        _articleTypeRepository = articleTypeRepository;
        _articleRepository = articleRepository;
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

        try
        {
            await _articleTypeRepository.AddAsync(model);
            await _unitOfWork.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Article type created";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_article_types_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "An article type with this name already exists.");
            return View(model);
        }
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

        try
        {
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "Article type updated";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_article_types_name") == true)
        {
            ModelState.AddModelError(nameof(model.Name), "An article type with this name already exists.");
            return View(model);
        }
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

        // Check if any articles use this type
        var articleCount = await _articleRepository.Query()
            .CountAsync(a => a.ArticleTypeId == id);

        if (articleCount > 0)
        {
            TempData["ErrorMessage"] = $"Cannot delete this article type because {articleCount} article(s) are using it. Please reassign those articles first.";
            return RedirectToAction(nameof(Index));
        }

        await _articleTypeRepository.DeleteAsync(articleType);
        await _unitOfWork.SaveChangesAsync();

        TempData["SuccessMessage"] = "Article type deleted";
        return RedirectToAction(nameof(Index));
    }
}

