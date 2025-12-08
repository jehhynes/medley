using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/fragments")]
[ApiController]
[Authorize]
public class FragmentsApiController : ControllerBase
{
    private readonly IRepository<Fragment> _fragmentRepository;

    public FragmentsApiController(IRepository<Fragment> fragmentRepository)
    {
        _fragmentRepository = fragmentRepository;
    }

    /// <summary>
    /// Get all fragments (stub implementation)
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            message = "Fragments API - Coming Soon",
            count = 0,
            items = new List<object>()
        });
    }

    /// <summary>
    /// Get a specific fragment by ID (stub implementation)
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult Get(Guid id)
    {
        return Ok(new
        {
            message = "Fragment detail - Coming Soon",
            id
        });
    }

    /// <summary>
    /// Get all fragments for a specific source
    /// </summary>
    [HttpGet("by-source/{sourceId}")]
    public async Task<IActionResult> GetBySourceId(Guid sourceId)
    {
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .Where(f => f.Source.Id == sourceId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.Title,
                f.Summary,
                f.Category,
                f.Content,
                f.CreatedAt,
                f.LastModifiedAt
            })
            .ToListAsync();

        return Ok(fragments);
    }
}

