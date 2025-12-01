using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/sources")]
[ApiController]
[Authorize]
public class SourcesApiController : ControllerBase
{
    private readonly IRepository<Source> _sourceRepository;

    public SourcesApiController(IRepository<Source> sourceRepository)
    {
        _sourceRepository = sourceRepository;
    }

    /// <summary>
    /// Get all sources
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var sources = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .OrderByDescending(s => s.Date)
            .Skip(skip)
            .Take(take)
            .Select(s => new
            {
                id = s.Id.ToString(),
                s.Name,
                type = s.Type.ToString(),
                s.Date,
                s.ExternalId,
                IntegrationName = s.Integration.DisplayName,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(sources);
    }

    /// <summary>
    /// Get a specific source by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var source = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .Include(s => s.Fragments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (source == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            source.Id,
            source.Name,
            Type = source.Type.ToString(),
            source.Date,
            source.Content,
            source.MetadataJson,
            source.ExternalId,
            IntegrationName = source.Integration.DisplayName,
            FragmentsCount = source.Fragments.Count,
            source.CreatedAt
        });
    }

    /// <summary>
    /// Search sources by name
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int take = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        var sources = await _sourceRepository.Query()
            .Where(s => s.Name != null && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Date)
            .Take(take)
            .Select(s => new
            {
                id = s.Id.ToString(),
                s.Name,
                type = s.Type.ToString(),
                s.Date
            })
            .ToListAsync();

        return Ok(sources);
    }
}

