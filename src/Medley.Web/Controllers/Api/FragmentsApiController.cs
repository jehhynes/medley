using Medley.Application.Configuration;
using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Application.Services;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Medley.Web.Controllers.Api;

[Route("api/fragments")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class FragmentsApiController : ControllerBase
{
    private readonly IFragmentRepository _fragmentRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IEmbeddingHelper _embeddingHelper;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly ILogger<FragmentsApiController> _logger;
    private readonly AiCallContext _aiCallContext;

    public FragmentsApiController(
        IFragmentRepository fragmentRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IEmbeddingHelper embeddingHelper,
        IOptions<EmbeddingSettings> embeddingSettings,
        ILogger<FragmentsApiController> logger,
        AiCallContext aiCallContext)
    {
        _fragmentRepository = fragmentRepository;
        _embeddingGenerator = embeddingGenerator;
        _embeddingHelper = embeddingHelper;
        _embeddingSettings = embeddingSettings.Value;
        _logger = logger;
        _aiCallContext = aiCallContext;
    }

    /// <summary>
    /// Get all fragments with pagination
    /// </summary>
    /// <param name="skip">Number of fragments to skip</param>
    /// <param name="take">Number of fragments to take</param>
    /// <returns>List of fragments</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<FragmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FragmentDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .OrderByDescending(f => f.Source!.Date)
            .ThenByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id) // Deterministic tiebreaker for pagination
            .Skip(skip)
            .Take(take)
            .Select(f => new FragmentDto
            {
                Id = f.Id,
                Title = f.Title,
                Summary = f.Summary,
                Category = f.Category,
                Content = f.Content,
                SourceId = f.Source == null ? null : (Guid?)f.Source.Id,
                SourceName = f.Source == null ? null : (string?)f.Source.Name,
                SourceType = f.Source == null ? null : (SourceType?)f.Source.Type,
                SourceDate = f.Source == null ? null : (DateTimeOffset?)f.Source.Date,
                CreatedAt = f.CreatedAt,
                Confidence = f.Confidence,
                ConfidenceComment = f.ConfidenceComment
            })
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Get a specific fragment by ID
    /// </summary>
    /// <param name="id">Fragment ID</param>
    /// <returns>Fragment details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(FragmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FragmentDto>> Get(Guid id)
    {
        var fragment = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fragment == null)
        {
            return NotFound();
        }

        return Ok(new FragmentDto
        {
            Id = fragment.Id,
            Title = fragment.Title,
            Summary = fragment.Summary,
            Category = fragment.Category,
            Content = fragment.Content,
            SourceId = fragment.Source == null ? null : (Guid?)fragment.Source.Id,
            SourceName = fragment.Source == null ? null : (string?)fragment.Source.Name,
            SourceType = fragment.Source == null ? null : (SourceType?)fragment.Source.Type,
            SourceDate = fragment.Source == null ? null : (DateTimeOffset?)fragment.Source.Date,
            CreatedAt = fragment.CreatedAt,
            LastModifiedAt = fragment.LastModifiedAt,
            Confidence = fragment.Confidence,
            ConfidenceComment = fragment.ConfidenceComment
        });
    }

    /// <summary>
    /// Get all fragments for a specific source
    /// </summary>
    /// <param name="sourceId">Source ID</param>
    /// <returns>List of fragments from the source</returns>
    [HttpGet("by-source/{sourceId}")]
    [ProducesResponseType(typeof(List<FragmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FragmentDto>>> GetBySourceId(Guid sourceId)
    {
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
            .Where(f => f.Source!.Id == sourceId)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id) // Deterministic tiebreaker
            .Select(f => new FragmentDto
            {
                Id = f.Id,
                Title = f.Title,
                Summary = f.Summary,
                Category = f.Category,
                Content = f.Content,
                SourceId = f.Source!.Id,
                SourceName = f.Source.Name,
                SourceType = f.Source.Type,
                SourceDate = f.Source.Date,
                CreatedAt = f.CreatedAt,
                LastModifiedAt = f.LastModifiedAt,
                Confidence = f.Confidence,
                ConfidenceComment = f.ConfidenceComment
            })
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Search fragments using semantic similarity (vector search)
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="take">Number of results to return</param>
    /// <returns>List of fragments with similarity scores</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<FragmentSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<FragmentSearchResult>>> Search([FromQuery] string query, [FromQuery] int take = 50)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest("Query parameter is required");
        }

        try
        {
            _logger.LogDebug("Performing semantic search with query: {Query}", query);

            // Generate embedding for the search query
            var options = new EmbeddingGenerationOptions
            {
                Dimensions = _embeddingSettings.Dimensions
            };
            GeneratedEmbeddings<Embedding<float>> embeddingResult;
            using (_aiCallContext.SetContext(nameof(FragmentsApiController), nameof(Search), null, null))
            {
                embeddingResult = await _embeddingGenerator.GenerateAsync(new[] { query }, options);
            }
            
            var embedding = embeddingResult.FirstOrDefault();
            if (embedding == null)
            {
                _logger.LogWarning("Failed to generate embedding for search query: {Query}", query);
                return StatusCode(500, new { message = "Failed to generate embedding for search query." });
            }
            
            // Process embedding (conditionally normalize based on model)
            var processedVector = _embeddingHelper.ProcessEmbedding(embedding.Vector.ToArray());
            
            // Find similar fragments using vector similarity
            var similarFragments = await _fragmentRepository.FindSimilarAsync(
                processedVector, 
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
                    return new FragmentSearchResult
                    {
                        Id = fragment.Id,
                        Title = fragment.Title,
                        Summary = fragment.Summary,
                        Category = fragment.Category,
                        SourceId = fragment.Source == null ? null : (Guid?)fragment.Source.Id,
                        SourceName = fragment.Source == null ? null : (string?)fragment.Source.Name,
                        SourceType = fragment.Source == null ? null : (SourceType?)fragment.Source.Type,
                        SourceDate = fragment.Source == null ? null : (DateTimeOffset?)fragment.Source.Date,
                        CreatedAt = fragment.CreatedAt,
                        Confidence = fragment.Confidence,
                        ConfidenceComment = fragment.ConfidenceComment,
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

    /// <summary>
    /// Get titles for multiple fragments by their IDs
    /// </summary>
    /// <param name="ids">List of fragment IDs</param>
    /// <returns>List of fragment titles</returns>
    [HttpPost("titles")]
    [ProducesResponseType(typeof(List<FragmentTitleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<FragmentTitleDto>>> GetTitles([FromBody] List<Guid> ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return BadRequest("At least one fragment ID is required");
        }

        try
        {
            var fragments = await _fragmentRepository.Query()
                .Where(f => ids.Contains(f.Id))
                .Select(f => new FragmentTitleDto
                {
                    Id = f.Id,
                    Title = f.Title
                })
                .ToListAsync();

            // Maintain the order of the input IDs
            var fragmentLookup = fragments.ToDictionary(f => f.Id);
            var orderedResults = ids
                .Where(id => fragmentLookup.ContainsKey(id))
                .Select(id => fragmentLookup[id])
                .ToList();

            return Ok(orderedResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fragment titles");
            return StatusCode(500, new { message = "Failed to retrieve fragment titles. Please try again." });
        }
    }
}
