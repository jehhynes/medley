using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Domain.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/templates")]
[ApiController]
[Authorize]
public class TemplatesApiController : ControllerBase
{
    private readonly IRepository<Template> _templateRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TemplatesApiController> _logger;

    public TemplatesApiController(
        IRepository<Template> templateRepository,
        IUnitOfWork unitOfWork,
        ILogger<TemplatesApiController> logger)
    {
        _templateRepository = templateRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all templates
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _templateRepository.Query()
            .OrderBy(t => t.Type)
            .ToListAsync();

        var templateDtos = templates.Select(t => new TemplateListDto
        {
            Id = t.Id,
            Name = t.Type.GetName(),
            Type = t.Type,
            TypeName = t.Type.ToString(),
            Description = t.Type.GetDescription(),
            CreatedAt = t.CreatedAt,
            LastModifiedAt = t.LastModifiedAt
        }).ToList();

        return Ok(templateDtos);
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var template = await _templateRepository.GetByIdAsync(id);

        if (template == null)
        {
            return NotFound();
        }

        return Ok(new TemplateDto
        {
            Id = template.Id,
            Name = template.Type.GetName(),
            Type = template.Type,
            TypeName = template.Type.ToString(),
            Description = template.Type.GetDescription(),
            Content = template.Content,
            CreatedAt = template.CreatedAt,
            LastModifiedAt = template.LastModifiedAt
        });
    }

    /// <summary>
    /// Update a template's content
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(id);

            if (template == null)
            {
                return NotFound();
            }

            if (request.Content != null)
            {
                template.Content = request.Content;
            }

            template.LastModifiedAt = DateTimeOffset.UtcNow;

            // Entity is already tracked, changes will be saved on SaveChangesAsync
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated template {TemplateId} ({TemplateName})", id, template.Type.GetName());

            return Ok(new TemplateDto
            {
                Id = template.Id,
                Name = template.Type.GetName(),
                Type = template.Type,
                TypeName = template.Type.ToString(),
                Description = template.Type.GetDescription(),
                Content = template.Content,
                CreatedAt = template.CreatedAt,
                LastModifiedAt = template.LastModifiedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update template {TemplateId}", id);
            return StatusCode(500, new { message = "Failed to update template" });
        }
    }

}

public class TemplateListDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public TemplateType Type { get; set; }
    public required string TypeName { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
}

public class TemplateDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public TemplateType Type { get; set; }
    public required string TypeName { get; set; }
    public string? Description { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastModifiedAt { get; set; }
}

public class UpdateTemplateRequest
{
    public string? Content { get; set; }
}

