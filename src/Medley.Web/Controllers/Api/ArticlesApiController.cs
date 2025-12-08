using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/articles")]
[ApiController]
[Authorize]
public class ArticlesApiController : ControllerBase
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ArticleType> _articleTypeRepository;

    public ArticlesApiController(
        IRepository<Article> articleRepository,
        IRepository<ArticleType> articleTypeRepository)
    {
        _articleRepository = articleRepository;
        _articleTypeRepository = articleTypeRepository;
    }

    /// <summary>
    /// Get all article types
    /// </summary>
    [HttpGet("types")]
    public async Task<IActionResult> GetArticleTypes()
    {
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .Select(at => new
            {
                at.Id,
                at.Name,
                at.Icon
            })
            .ToListAsync();

        return Ok(articleTypes);
    }

    /// <summary>
    /// Get all articles as a tree structure
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetTree()
    {
        var articles = await _articleRepository.Query()
            .Include(a => a.ChildArticles)
            .Include(a => a.ArticleType)
            .OrderBy(a => a.Title)
            .ToListAsync();

        var tree = BuildTree(articles, null);
        return Ok(tree);
    }

    /// <summary>
    /// Get a specific article by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var article = await _articleRepository.Query()
            .Include(a => a.ChildArticles)
            .Include(a => a.ParentArticle)
            .Include(a => a.Fragments)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            article.Id,
            article.Title,
            article.Content,
            article.Status,
            article.PublishedAt,
            article.CreatedAt,
            article.ParentArticleId,
            ParentTitle = article.ParentArticle?.Title,
            ChildrenCount = article.ChildArticles.Count,
            FragmentsCount = article.Fragments.Count
        });
    }

    /// <summary>
    /// Get children of a specific article
    /// </summary>
    [HttpGet("{id}/children")]
    public async Task<IActionResult> GetChildren(Guid id)
    {
        var children = await _articleRepository.Query()
            .Where(a => a.ParentArticleId == id)
            .OrderBy(a => a.Title)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Status,
                a.CreatedAt,
                ChildrenCount = a.ChildArticles.Count
            })
            .ToListAsync();

        return Ok(children);
    }

    /// <summary>
    /// Create a new article
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArticleRequest request)
    {
        var article = new Article
        {
            Title = request.Title,
            Status = Domain.Enums.ArticleStatus.Draft,
            ParentArticleId = request.ParentArticleId
        };

        await _articleRepository.SaveAsync(article);
        return CreatedAtAction(nameof(Get), new { id = article.Id }, article);
    }

    /// <summary>
    /// Update an existing article
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateArticleRequest request)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        article.Title = request.Title;
        if (request.Status.HasValue)
        {
            article.Status = request.Status.Value;
        }
        if (request.Content != null)
        {
            article.Content = request.Content;
        }

        await _articleRepository.SaveAsync(article);
        return Ok(article);
    }

    /// <summary>
    /// Delete an article
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var article = await _articleRepository.GetByIdAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        // Note: This is a simplified delete. In production, you'd want to handle child articles
        // and potentially implement soft delete
        return Ok(new { message = "Delete functionality to be implemented" });
    }

    /// <summary>
    /// Helper method to build tree structure
    /// </summary>
    private List<object> BuildTree(List<Article> allArticles, Guid? parentId)
    {
        return allArticles
            .Where(a => a.ParentArticleId == parentId)
            .Select(a => new
            {
                id = a.Id.ToString(),
                a.Title,
                a.Status,
                a.CreatedAt,
                articleTypeIcon = a.ArticleType?.Icon ?? "bi-file-text",
                children = BuildTree(allArticles, a.Id)
            })
            .ToList<object>();
    }
}

public class CreateArticleRequest
{
    public required string Title { get; set; }
    public Guid? ParentArticleId { get; set; }
}

public class UpdateArticleRequest
{
    public required string Title { get; set; }
    public Domain.Enums.ArticleStatus? Status { get; set; }
    public string? Content { get; set; }
}

