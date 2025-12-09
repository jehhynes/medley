using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace Medley.Web.Controllers.Api;

[Route("api/fragments")]
[ApiController]
[Authorize]
public class FragmentsApiController : ControllerBase
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly ILogger<FragmentsApiController> _logger;

    public FragmentsApiController(
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<FragmentsApiController> logger)
    {
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Get all fragments with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .OrderByDescending(f => f.Source.Date)
            .ThenByDescending(f => f.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(f => new
            {
                f.Id,
                f.Title,
                f.Summary,
                f.Category,
                SourceId = f.Source.Id,
                SourceName = f.Source.Name,
                SourceType = f.Source.Type,
                SourceDate = f.Source.Date,
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Get a specific fragment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var fragment = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fragment == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            fragment.Id,
            fragment.Title,
            fragment.Summary,
            fragment.Category,
            fragment.Content,
            SourceId = fragment.Source.Id,
            SourceName = fragment.Source.Name,
            SourceType = fragment.Source.Type,
            SourceDate = fragment.Source.Date,
            fragment.CreatedAt,
            fragment.LastModifiedAt
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

    /// <summary>
    /// Search fragments using semantic similarity (vector search)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int take = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        try
        {
            _logger.LogDebug("Performing semantic search with query: {Query}", query);

            // Generate embedding for the search query
            var options = new EmbeddingGenerationOptions()
            {
                Dimensions = 2000
            };
            
            var embedding = await _embeddingGenerator.GenerateVectorAsync(query, options);
            
            // Find similar fragments using vector similarity
            var similarFragments = await _fragmentRepository.FindSimilarAsync(
                embedding.ToArray(), 
                take);

            // Load related data and map to response
            var fragmentIds = similarFragments.Select(r => r.Fragment.Id).ToList();
            
            var fragmentsWithSource = await _fragmentRepository.Query()
                .Include(f => f.Source)
                .Where(f => fragmentIds.Contains(f.Id))
                .ToListAsync();

            // Maintain the similarity order from the vector search
            var fragmentLookup = fragmentsWithSource.ToDictionary(f => f.Id);
            var results = similarFragments
                .Where(r => fragmentLookup.ContainsKey(r.Fragment.Id))
                .Select(result =>
                {
                    var fragment = fragmentLookup[result.Fragment.Id];
                    return new
                    {
                        fragment.Id,
                        fragment.Title,
                        fragment.Summary,
                        fragment.Category,
                        SourceId = fragment.Source.Id,
                        SourceName = fragment.Source.Name,
                        SourceType = fragment.Source.Type,
                        SourceDate = fragment.Source.Date,
                        fragment.CreatedAt,
                        Similarity = 1 - (result.Distance / 2) // Convert L2 distance to similarity score (0-1)
                    };
                })
                .ToList();

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search");
            return StatusCode(500, new { message = "Failed to perform search. Please try again." });
        }
    }
}
