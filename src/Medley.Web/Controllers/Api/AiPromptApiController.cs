using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Domain.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/prompts")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class AiPromptApiController : ControllerBase
{
    private readonly IRepository<AiPrompt> _promptRepository;
    private readonly IRepository<ArticleType> _articleTypeRepository;
    private readonly IRepository<FragmentCategory> _fragmentCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiPromptApiController> _logger;

    public AiPromptApiController(
        IRepository<AiPrompt> promptRepository,
        IRepository<ArticleType> articleTypeRepository,
        IRepository<FragmentCategory> fragmentCategoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<AiPromptApiController> logger)
    {
        _promptRepository = promptRepository;
        _articleTypeRepository = articleTypeRepository;
        _fragmentCategoryRepository = fragmentCategoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all possible templates (derived from PromptType enum)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AiPromptListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AiPromptListDto>>> GetAll()
    {
        // Get all existing templates from database
        var existingTemplates = await _promptRepository.Query()
            .Include(t => t.ArticleType)
            .Include(t => t.FragmentCategory)
            .ToListAsync();

        // Get all article types
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .ToListAsync();

        // Get all fragment categories
        var fragmentCategories = await _fragmentCategoryRepository.Query()
            .OrderBy(fc => fc.Name)
            .ToListAsync();

        var result = new List<AiPromptListDto>();

        // Iterate through all PromptType enum values
        foreach (PromptType promptType in Enum.GetValues(typeof(PromptType)))
        {
            var isPerArticleType = promptType.GetIsPerArticleType();
            var isPerFragmentCategory = promptType.GetIsPerFragmentCategory();

            if (isPerArticleType)
            {
                // Create one entry per article type
                foreach (var articleType in articleTypes)
                {
                    var existingTemplate = existingTemplates.FirstOrDefault(t =>
                        t.Type == promptType && t.ArticleTypeId == articleType.Id);

                    result.Add(new AiPromptListDto
                    {
                        Id = existingTemplate?.Id,
                        Name = promptType.GetName(),
                        Type = promptType,
                        Description = promptType.GetDescription(),
                        IsPerArticleType = true,
                        ArticleTypeId = articleType.Id,
                        ArticleTypeName = articleType.Name,
                        IsPerFragmentCategory = false,
                        FragmentCategoryId = null,
                        FragmentCategoryName = null,
                        Exists = existingTemplate != null,
                        CreatedAt = existingTemplate?.CreatedAt,
                        LastModifiedAt = existingTemplate?.LastModifiedAt
                    });
                }
            }
            else if (isPerFragmentCategory)
            {
                // Create one entry per fragment category
                foreach (var fragmentCategory in fragmentCategories)
                {
                    var existingTemplate = existingTemplates.FirstOrDefault(t =>
                        t.Type == promptType && t.FragmentCategoryId == fragmentCategory.Id);

                    result.Add(new AiPromptListDto
                    {
                        Id = existingTemplate?.Id,
                        Name = promptType.GetName(),
                        Type = promptType,
                        Description = promptType.GetDescription(),
                        IsPerArticleType = false,
                        ArticleTypeId = null,
                        ArticleTypeName = null,
                        IsPerFragmentCategory = true,
                        FragmentCategoryId = fragmentCategory.Id,
                        FragmentCategoryName = fragmentCategory.Name,
                        Exists = existingTemplate != null,
                        CreatedAt = existingTemplate?.CreatedAt,
                        LastModifiedAt = existingTemplate?.LastModifiedAt
                    });
                }
            }
            else
            {
                // Create one entry for non-per-article-type and non-per-fragment-category templates
                var existingTemplate = existingTemplates.FirstOrDefault(t =>
                    t.Type == promptType && t.ArticleTypeId == null && t.FragmentCategoryId == null);

                result.Add(new AiPromptListDto
                {
                    Id = existingTemplate?.Id,
                    Name = promptType.GetName(),
                    Type = promptType,
                    Description = promptType.GetDescription(),
                    IsPerArticleType = false,
                    ArticleTypeId = null,
                    ArticleTypeName = null,
                    IsPerFragmentCategory = false,
                    FragmentCategoryId = null,
                    FragmentCategoryName = null,
                    Exists = existingTemplate != null,
                    CreatedAt = existingTemplate?.CreatedAt,
                    LastModifiedAt = existingTemplate?.LastModifiedAt
                });
            }
        }

        // Sort: non-per-type first (alphabetically), then per-article-type, then per-fragment-category (alphabetically)
        result = result
            .OrderBy(t => t.IsPerArticleType || t.IsPerFragmentCategory)
            .ThenBy(t => t.IsPerFragmentCategory)
            .ThenBy(t => t.Name)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get a specific template by type and optional article type or fragment category
    /// </summary>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(AiPromptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiPromptDto>> Get(
        PromptType type,
        [FromQuery] Guid? articleTypeId = null,
        [FromQuery] Guid? fragmentCategoryId = null)
    {
        var isPerArticleType = type.GetIsPerArticleType();
        var isPerFragmentCategory = type.GetIsPerFragmentCategory();

        // Validate that articleTypeId is provided for per-article-type templates
        if (isPerArticleType && !articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
        }

        // Validate that fragmentCategoryId is provided for per-fragment-category templates
        if (isPerFragmentCategory && !fragmentCategoryId.HasValue)
        {
            return BadRequest(new { message = "fragmentCategoryId is required for per-fragment-category templates" });
        }

        // Validate that articleTypeId is NOT provided for non-per-article-type templates
        if (!isPerArticleType && articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
        }

        // Validate that fragmentCategoryId is NOT provided for non-per-fragment-category templates
        if (!isPerFragmentCategory && fragmentCategoryId.HasValue)
        {
            return BadRequest(new { message = "fragmentCategoryId should not be provided for this template type" });
        }

        // Look up the template
        var template = await _promptRepository.Query()
            .Include(t => t.ArticleType)
            .Include(t => t.FragmentCategory)
            .FirstOrDefaultAsync(t =>
                t.Type == type &&
                t.ArticleTypeId == articleTypeId &&
                t.FragmentCategoryId == fragmentCategoryId);

        string? articleTypeName = null;
        if (articleTypeId.HasValue)
        {
            var articleType = await _articleTypeRepository.GetByIdAsync(articleTypeId.Value);
            articleTypeName = articleType?.Name;
        }

        string? fragmentCategoryName = null;
        if (fragmentCategoryId.HasValue)
        {
            var fragmentCategory = await _fragmentCategoryRepository.GetByIdAsync(fragmentCategoryId.Value);
            fragmentCategoryName = fragmentCategory?.Name;
        }

        if (template != null)
        {
            // Template exists - return with content
            return Ok(new AiPromptDto
            {
                Id = template.Id,
                Name = type.GetName(),
                Type = type,
                Description = type.GetDescription(),
                IsPerArticleType = isPerArticleType,
                ArticleTypeId = articleTypeId,
                ArticleTypeName = articleTypeName,
                IsPerFragmentCategory = isPerFragmentCategory,
                FragmentCategoryId = fragmentCategoryId,
                FragmentCategoryName = fragmentCategoryName,
                Content = template.Content,
                Exists = true,
                CreatedAt = template.CreatedAt,
                LastModifiedAt = template.LastModifiedAt
            });
        }
        else
        {
            // Template doesn't exist - return metadata with empty content
            return Ok(new AiPromptDto
            {
                Id = null,
                Name = type.GetName(),
                Type = type,
                Description = type.GetDescription(),
                IsPerArticleType = isPerArticleType,
                ArticleTypeId = articleTypeId,
                ArticleTypeName = articleTypeName,
                IsPerFragmentCategory = isPerFragmentCategory,
                FragmentCategoryId = fragmentCategoryId,
                FragmentCategoryName = fragmentCategoryName,
                Content = string.Empty,
                Exists = false,
                CreatedAt = null,
                LastModifiedAt = null
            });
        }
    }

    /// <summary>
    /// Create or update a template
    /// </summary>
    [HttpPut("{type}")]
    [ProducesResponseType(typeof(AiPromptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AiPromptDto>> CreateOrUpdate(
        PromptType type,
        [FromQuery] Guid? articleTypeId,
        [FromQuery] Guid? fragmentCategoryId,
        [FromBody] CreateOrUpdateAiPromptRequest request)
    {
        try
        {
            var isPerArticleType = type.GetIsPerArticleType();
            var isPerFragmentCategory = type.GetIsPerFragmentCategory();

            // Validate that articleTypeId is provided for per-article-type templates
            if (isPerArticleType && !articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
            }

            // Validate that fragmentCategoryId is provided for per-fragment-category templates
            if (isPerFragmentCategory && !fragmentCategoryId.HasValue)
            {
                return BadRequest(new { message = "fragmentCategoryId is required for per-fragment-category templates" });
            }

            // Validate that articleTypeId is NOT provided for non-per-article-type templates
            if (!isPerArticleType && articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
            }

            // Validate that fragmentCategoryId is NOT provided for non-per-fragment-category templates
            if (!isPerFragmentCategory && fragmentCategoryId.HasValue)
            {
                return BadRequest(new { message = "fragmentCategoryId should not be provided for this template type" });
            }

            // Look up existing template
            var template = await _promptRepository.Query()
                .Include(t => t.ArticleType)
                .Include(t => t.FragmentCategory)
                .FirstOrDefaultAsync(t =>
                    t.Type == type &&
                    t.ArticleTypeId == articleTypeId &&
                    t.FragmentCategoryId == fragmentCategoryId);

            string? articleTypeName = null;
            if (articleTypeId.HasValue)
            {
                var articleType = await _articleTypeRepository.GetByIdAsync(articleTypeId.Value);
                if (articleType == null)
                {
                    return BadRequest(new { message = "Invalid articleTypeId" });
                }
                articleTypeName = articleType.Name;
            }

            string? fragmentCategoryName = null;
            if (fragmentCategoryId.HasValue)
            {
                var fragmentCategory = await _fragmentCategoryRepository.GetByIdAsync(fragmentCategoryId.Value);
                if (fragmentCategory == null)
                {
                    return BadRequest(new { message = "Invalid fragmentCategoryId" });
                }
                fragmentCategoryName = fragmentCategory.Name;
            }

            if (template != null)
            {
                // Update existing template
                template.Content = request.Content;
                template.LastModifiedAt = DateTimeOffset.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated template {PromptType}" +
                    (articleTypeId.HasValue ? " for article type {ArticleTypeId}" : "") +
                    (fragmentCategoryId.HasValue ? " for fragment category {FragmentCategoryId}" : ""),
                    type, articleTypeId, fragmentCategoryId);

                return Ok(new AiPromptDto
                {
                    Id = template.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    IsPerFragmentCategory = isPerFragmentCategory,
                    FragmentCategoryId = fragmentCategoryId,
                    FragmentCategoryName = fragmentCategoryName,
                    Content = template.Content,
                    Exists = true,
                    CreatedAt = template.CreatedAt,
                    LastModifiedAt = template.LastModifiedAt
                });
            }
            else
            {
                // Create new template
                var newTemplate = new AiPrompt
                {
                    Type = type,
                    Content = request.Content,
                    ArticleTypeId = articleTypeId,
                    FragmentCategoryId = fragmentCategoryId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                await _promptRepository.AddAsync(newTemplate);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Created template {PromptType}" +
                    (articleTypeId.HasValue ? " for article type {ArticleTypeId}" : "") +
                    (fragmentCategoryId.HasValue ? " for fragment category {FragmentCategoryId}" : ""),
                    type, articleTypeId, fragmentCategoryId);

                return Ok(new AiPromptDto
                {
                    Id = newTemplate.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    IsPerFragmentCategory = isPerFragmentCategory,
                    FragmentCategoryId = fragmentCategoryId,
                    FragmentCategoryName = fragmentCategoryName,
                    Content = newTemplate.Content,
                    Exists = true,
                    CreatedAt = newTemplate.CreatedAt,
                    LastModifiedAt = newTemplate.LastModifiedAt
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update template {PromptType}", type);
            return StatusCode(500, new { message = "Failed to create or update template" });
        }
    }

    /// <summary>
    /// Get all article types
    /// </summary>
    [HttpGet("article-types")]
    [ProducesResponseType(typeof(List<ArticleTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ArticleTypeDto>>> GetArticleTypes()
    {
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .Select(at => new ArticleTypeDto
            {
                Id = at.Id,
                Name = at.Name,
                Icon = at.Icon
            })
            .ToListAsync();

        return Ok(articleTypes);
    }

    /// <summary>
    /// Get all fragment categories
    /// </summary>
    [HttpGet("fragment-categories")]
    [ProducesResponseType(typeof(List<FragmentCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FragmentCategoryDto>>> GetFragmentCategories()
    {
        var fragmentCategories = await _fragmentCategoryRepository.Query()
            .OrderBy(fc => fc.Name)
            .Select(fc => new FragmentCategoryDto
            {
                Id = fc.Id,
                Name = fc.Name,
                Icon = fc.Icon
            })
            .ToListAsync();

        return Ok(fragmentCategories);
    }
}

public class CreateOrUpdateAiPromptRequest
{
    public required string Content { get; set; }
}

