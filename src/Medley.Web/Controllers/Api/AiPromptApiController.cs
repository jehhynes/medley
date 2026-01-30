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
    private readonly IRepository<KnowledgeCategory> _knowledgeCategoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AiPromptApiController> _logger;

    public AiPromptApiController(
        IRepository<AiPrompt> promptRepository,
        IRepository<ArticleType> articleTypeRepository,
        IRepository<KnowledgeCategory> knowledgeCategoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<AiPromptApiController> logger)
    {
        _promptRepository = promptRepository;
        _articleTypeRepository = articleTypeRepository;
        _knowledgeCategoryRepository = knowledgeCategoryRepository;
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
            .Include(t => t.KnowledgeCategory)
            .ToListAsync();

        // Get all article types
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .ToListAsync();

        // Get all knowledge categories
        var knowledgeCategories = await _knowledgeCategoryRepository.Query()
            .OrderBy(fc => fc.Name)
            .ToListAsync();

        var result = new List<AiPromptListDto>();

        // Iterate through all PromptType enum values
        foreach (PromptType promptType in Enum.GetValues(typeof(PromptType)))
        {
            var isPerArticleType = promptType.GetIsPerArticleType();
            var isPerKnowledgeCategory = promptType.GetIsPerKnowledgeCategory();

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
                        IsPerKnowledgeCategory = false,
                        KnowledgeCategoryId = null,
                        KnowledgeCategoryName = null,
                        Exists = existingTemplate != null,
                        CreatedAt = existingTemplate?.CreatedAt,
                        LastModifiedAt = existingTemplate?.LastModifiedAt
                    });
                }
            }
            else if (isPerKnowledgeCategory)
            {
                // Create one entry per knowledge category
                foreach (var knowledgeCategory in knowledgeCategories)
                {
                    var existingTemplate = existingTemplates.FirstOrDefault(t =>
                        t.Type == promptType && t.KnowledgeCategoryId == knowledgeCategory.Id);

                    result.Add(new AiPromptListDto
                    {
                        Id = existingTemplate?.Id,
                        Name = promptType.GetName(),
                        Type = promptType,
                        Description = promptType.GetDescription(),
                        IsPerArticleType = false,
                        ArticleTypeId = null,
                        ArticleTypeName = null,
                        IsPerKnowledgeCategory = true,
                        KnowledgeCategoryId = knowledgeCategory.Id,
                        KnowledgeCategoryName = knowledgeCategory.Name,
                        Exists = existingTemplate != null,
                        CreatedAt = existingTemplate?.CreatedAt,
                        LastModifiedAt = existingTemplate?.LastModifiedAt
                    });
                }
            }
            else
            {
                // Create one entry for non-per-article-type and non-per-knowledge-category templates
                var existingTemplate = existingTemplates.FirstOrDefault(t =>
                    t.Type == promptType && t.ArticleTypeId == null && t.KnowledgeCategoryId == null);

                result.Add(new AiPromptListDto
                {
                    Id = existingTemplate?.Id,
                    Name = promptType.GetName(),
                    Type = promptType,
                    Description = promptType.GetDescription(),
                    IsPerArticleType = false,
                    ArticleTypeId = null,
                    ArticleTypeName = null,
                    IsPerKnowledgeCategory = false,
                    KnowledgeCategoryId = null,
                    KnowledgeCategoryName = null,
                    Exists = existingTemplate != null,
                    CreatedAt = existingTemplate?.CreatedAt,
                    LastModifiedAt = existingTemplate?.LastModifiedAt
                });
            }
        }

        // Sort: non-per-type first (alphabetically), then per-article-type, then per-knowledge-category (alphabetically)
        result = result
            .OrderBy(t => t.IsPerArticleType || t.IsPerKnowledgeCategory)
            .ThenBy(t => t.IsPerKnowledgeCategory)
            .ThenBy(t => t.Name)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get a specific template by type and optional article type or knowledge category
    /// </summary>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(AiPromptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AiPromptDto>> Get(
        PromptType type,
        [FromQuery] Guid? articleTypeId = null,
        [FromQuery] Guid? knowledgeCategoryId = null)
    {
        var isPerArticleType = type.GetIsPerArticleType();
        var isPerKnowledgeCategory = type.GetIsPerKnowledgeCategory();

        // Validate that articleTypeId is provided for per-article-type templates
        if (isPerArticleType && !articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
        }

        // Validate that knowledgeCategoryId is provided for per-knowledge-category templates
        if (isPerKnowledgeCategory && !knowledgeCategoryId.HasValue)
        {
            return BadRequest(new { message = "knowledgeCategoryId is required for per-knowledge-category templates" });
        }

        // Validate that articleTypeId is NOT provided for non-per-article-type templates
        if (!isPerArticleType && articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
        }

        // Validate that knowledgeCategoryId is NOT provided for non-per-knowledge-category templates
        if (!isPerKnowledgeCategory && knowledgeCategoryId.HasValue)
        {
            return BadRequest(new { message = "knowledgeCategoryId should not be provided for this template type" });
        }

        // Look up the template
        var template = await _promptRepository.Query()
            .Include(t => t.ArticleType)
            .Include(t => t.KnowledgeCategory)
            .FirstOrDefaultAsync(t =>
                t.Type == type &&
                t.ArticleTypeId == articleTypeId &&
                t.KnowledgeCategoryId == knowledgeCategoryId);

        string? articleTypeName = null;
        if (articleTypeId.HasValue)
        {
            var articleType = await _articleTypeRepository.GetByIdAsync(articleTypeId.Value);
            articleTypeName = articleType?.Name;
        }

        string? knowledgeCategoryName = null;
        if (knowledgeCategoryId.HasValue)
        {
            var knowledgeCategory = await _knowledgeCategoryRepository.GetByIdAsync(knowledgeCategoryId.Value);
            knowledgeCategoryName = knowledgeCategory?.Name;
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
                IsPerKnowledgeCategory = isPerKnowledgeCategory,
                KnowledgeCategoryId = knowledgeCategoryId,
                KnowledgeCategoryName = knowledgeCategoryName,
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
                IsPerKnowledgeCategory = isPerKnowledgeCategory,
                KnowledgeCategoryId = knowledgeCategoryId,
                KnowledgeCategoryName = knowledgeCategoryName,
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
        [FromQuery] Guid? knowledgeCategoryId,
        [FromBody] CreateOrUpdateAiPromptRequest request)
    {
        try
        {
            var isPerArticleType = type.GetIsPerArticleType();
            var isPerKnowledgeCategory = type.GetIsPerKnowledgeCategory();

            // Validate that articleTypeId is provided for per-article-type templates
            if (isPerArticleType && !articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
            }

            // Validate that knowledgeCategoryId is provided for per-knowledge-category templates
            if (isPerKnowledgeCategory && !knowledgeCategoryId.HasValue)
            {
                return BadRequest(new { message = "knowledgeCategoryId is required for per-knowledge-category templates" });
            }

            // Validate that articleTypeId is NOT provided for non-per-article-type templates
            if (!isPerArticleType && articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
            }

            // Validate that knowledgeCategoryId is NOT provided for non-per-knowledge-category templates
            if (!isPerKnowledgeCategory && knowledgeCategoryId.HasValue)
            {
                return BadRequest(new { message = "knowledgeCategoryId should not be provided for this template type" });
            }

            // Look up existing template
            var template = await _promptRepository.Query()
                .Include(t => t.ArticleType)
                .Include(t => t.KnowledgeCategory)
                .FirstOrDefaultAsync(t =>
                    t.Type == type &&
                    t.ArticleTypeId == articleTypeId &&
                    t.KnowledgeCategoryId == knowledgeCategoryId);

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

            string? knowledgeCategoryName = null;
            if (knowledgeCategoryId.HasValue)
            {
                var knowledgeCategory = await _knowledgeCategoryRepository.GetByIdAsync(knowledgeCategoryId.Value);
                if (knowledgeCategory == null)
                {
                    return BadRequest(new { message = "Invalid knowledgeCategoryId" });
                }
                knowledgeCategoryName = knowledgeCategory.Name;
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
                    (knowledgeCategoryId.HasValue ? " for knowledge category {KnowledgeCategoryId}" : ""),
                    type, articleTypeId, knowledgeCategoryId);

                return Ok(new AiPromptDto
                {
                    Id = template.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    IsPerKnowledgeCategory = isPerKnowledgeCategory,
                    KnowledgeCategoryId = knowledgeCategoryId,
                    KnowledgeCategoryName = knowledgeCategoryName,
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
                    KnowledgeCategoryId = knowledgeCategoryId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                await _promptRepository.AddAsync(newTemplate);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Created template {PromptType}" +
                    (articleTypeId.HasValue ? " for article type {ArticleTypeId}" : "") +
                    (knowledgeCategoryId.HasValue ? " for knowledge category {KnowledgeCategoryId}" : ""),
                    type, articleTypeId, knowledgeCategoryId);

                return Ok(new AiPromptDto
                {
                    Id = newTemplate.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    IsPerKnowledgeCategory = isPerKnowledgeCategory,
                    KnowledgeCategoryId = knowledgeCategoryId,
                    KnowledgeCategoryName = knowledgeCategoryName,
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
    /// Get all knowledge categories
    /// </summary>
    [HttpGet("knowledge-categories")]
    [ProducesResponseType(typeof(List<KnowledgeCategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeCategoryDto>>> GetKnowledgeCategories()
    {
        var knowledgeCategories = await _knowledgeCategoryRepository.Query()
            .OrderBy(fc => fc.Name)
            .Select(fc => new KnowledgeCategoryDto
            {
                Id = fc.Id,
                Name = fc.Name,
                Icon = fc.Icon
            })
            .ToListAsync();

        return Ok(knowledgeCategories);
    }
}

public class CreateOrUpdateAiPromptRequest
{
    public required string Content { get; set; }
}

