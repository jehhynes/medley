using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Domain.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/templates")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class TemplatesApiController : ControllerBase
{
    private readonly IRepository<Template> _templateRepository;
    private readonly IRepository<ArticleType> _articleTypeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TemplatesApiController> _logger;

    public TemplatesApiController(
        IRepository<Template> templateRepository,
        IRepository<ArticleType> articleTypeRepository,
        IUnitOfWork unitOfWork,
        ILogger<TemplatesApiController> logger)
    {
        _templateRepository = templateRepository;
        _articleTypeRepository = articleTypeRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all possible templates (derived from TemplateType enum)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TemplateListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TemplateListDto>>> GetAll()
    {
        // Get all existing templates from database
        var existingTemplates = await _templateRepository.Query()
            .Include(t => t.ArticleType)
            .ToListAsync();

        // Get all article types
        var articleTypes = await _articleTypeRepository.Query()
            .OrderBy(at => at.Name)
            .ToListAsync();

        var result = new List<TemplateListDto>();

        // Iterate through all TemplateType enum values
        foreach (TemplateType templateType in Enum.GetValues(typeof(TemplateType)))
        {
            var isPerArticleType = templateType.GetIsPerArticleType();

            if (isPerArticleType)
            {
                // Create one entry per article type
                foreach (var articleType in articleTypes)
                {
                    var existingTemplate = existingTemplates.FirstOrDefault(t =>
                        t.Type == templateType && t.ArticleTypeId == articleType.Id);

                    result.Add(new TemplateListDto
                    {
                        Id = existingTemplate?.Id,
                        Name = templateType.GetName(),
                        Type = templateType,
                        Description = templateType.GetDescription(),
                        IsPerArticleType = true,
                        ArticleTypeId = articleType.Id,
                        ArticleTypeName = articleType.Name,
                        Exists = existingTemplate != null,
                        CreatedAt = existingTemplate?.CreatedAt,
                        LastModifiedAt = existingTemplate?.LastModifiedAt
                    });
                }
            }
            else
            {
                // Create one entry for non-per-article-type templates
                var existingTemplate = existingTemplates.FirstOrDefault(t =>
                    t.Type == templateType && t.ArticleTypeId == null);

                result.Add(new TemplateListDto
                {
                    Id = existingTemplate?.Id,
                    Name = templateType.GetName(),
                    Type = templateType,
                    Description = templateType.GetDescription(),
                    IsPerArticleType = false,
                    ArticleTypeId = null,
                    ArticleTypeName = null,
                    Exists = existingTemplate != null,
                    CreatedAt = existingTemplate?.CreatedAt,
                    LastModifiedAt = existingTemplate?.LastModifiedAt
                });
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get a specific template by type and optional article type
    /// </summary>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TemplateDto>> Get(TemplateType type, [FromQuery] Guid? articleTypeId = null)
    {
        var isPerArticleType = type.GetIsPerArticleType();

        // Validate that articleTypeId is provided for per-article-type templates
        if (isPerArticleType && !articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
        }

        // Validate that articleTypeId is NOT provided for non-per-article-type templates
        if (!isPerArticleType && articleTypeId.HasValue)
        {
            return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
        }

        // Look up the template
        var template = await _templateRepository.Query()
            .Include(t => t.ArticleType)
            .FirstOrDefaultAsync(t =>
                t.Type == type &&
                t.ArticleTypeId == articleTypeId);

        string? articleTypeName = null;
        if (articleTypeId.HasValue)
        {
            var articleType = await _articleTypeRepository.GetByIdAsync(articleTypeId.Value);
            articleTypeName = articleType?.Name;
        }

        if (template != null)
        {
            // Template exists - return with content
            return Ok(new TemplateDto
            {
                Id = template.Id,
                Name = type.GetName(),
                Type = type,
                Description = type.GetDescription(),
                IsPerArticleType = isPerArticleType,
                ArticleTypeId = articleTypeId,
                ArticleTypeName = articleTypeName,
                Content = template.Content,
                Exists = true,
                CreatedAt = template.CreatedAt,
                LastModifiedAt = template.LastModifiedAt
            });
        }
        else
        {
            // Template doesn't exist - return metadata with empty content
            return Ok(new TemplateDto
            {
                Id = null,
                Name = type.GetName(),
                Type = type,
                Description = type.GetDescription(),
                IsPerArticleType = isPerArticleType,
                ArticleTypeId = articleTypeId,
                ArticleTypeName = articleTypeName,
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
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TemplateDto>> CreateOrUpdate(
        TemplateType type,
        [FromQuery] Guid? articleTypeId,
        [FromBody] CreateOrUpdateTemplateRequest request)
    {
        try
        {
            var isPerArticleType = type.GetIsPerArticleType();

            // Validate that articleTypeId is provided for per-article-type templates
            if (isPerArticleType && !articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId is required for per-article-type templates" });
            }

            // Validate that articleTypeId is NOT provided for non-per-article-type templates
            if (!isPerArticleType && articleTypeId.HasValue)
            {
                return BadRequest(new { message = "articleTypeId should not be provided for this template type" });
            }

            // Look up existing template
            var template = await _templateRepository.Query()
                .Include(t => t.ArticleType)
                .FirstOrDefaultAsync(t =>
                    t.Type == type &&
                    t.ArticleTypeId == articleTypeId);

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

            if (template != null)
            {
                // Update existing template
                template.Content = request.Content;
                template.LastModifiedAt = DateTimeOffset.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated template {TemplateType}" +
                    (articleTypeId.HasValue ? " for article type {ArticleTypeId}" : ""),
                    type, articleTypeId);

                return Ok(new TemplateDto
                {
                    Id = template.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    Content = template.Content,
                    Exists = true,
                    CreatedAt = template.CreatedAt,
                    LastModifiedAt = template.LastModifiedAt
                });
            }
            else
            {
                // Create new template
                var newTemplate = new Template
                {
                    Type = type,
                    Content = request.Content,
                    ArticleTypeId = articleTypeId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastModifiedAt = DateTimeOffset.UtcNow
                };

                await _templateRepository.AddAsync(newTemplate);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Created template {TemplateType}" +
                    (articleTypeId.HasValue ? " for article type {ArticleTypeId}" : ""),
                    type, articleTypeId);

                return Ok(new TemplateDto
                {
                    Id = newTemplate.Id,
                    Name = type.GetName(),
                    Type = type,
                    Description = type.GetDescription(),
                    IsPerArticleType = isPerArticleType,
                    ArticleTypeId = articleTypeId,
                    ArticleTypeName = articleTypeName,
                    Content = newTemplate.Content,
                    Exists = true,
                    CreatedAt = newTemplate.CreatedAt,
                    LastModifiedAt = newTemplate.LastModifiedAt
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create or update template {TemplateType}", type);
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
}

public class CreateOrUpdateTemplateRequest
{
    public required string Content { get; set; }
}

