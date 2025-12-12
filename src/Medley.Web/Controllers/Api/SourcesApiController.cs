using Medley.Application.Interfaces;
using Medley.Application.Jobs;
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
    private readonly IRepository<TagType> _tagTypeRepository;
    private readonly ITaggingService _taggingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<SourcesApiController> _logger;

    public SourcesApiController(
        IRepository<Source> sourceRepository,
        IRepository<TagType> tagTypeRepository,
        ITaggingService taggingService,
        IUnitOfWork unitOfWork,
        IBackgroundJobService backgroundJobService,
        ILogger<SourcesApiController> logger)
    {
        _sourceRepository = sourceRepository;
        _tagTypeRepository = tagTypeRepository;
        _taggingService = taggingService;
        _unitOfWork = unitOfWork;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Get all sources
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var sources = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .Include(s => s.Fragments)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagType)
            .OrderByDescending(s => s.Date)
            .Skip(skip)
            .Take(take)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.Date,
                s.ExternalId,
                IntegrationName = s.Integration.DisplayName,
                s.IsInternal,
                FragmentsCount = s.Fragments.Count,
                ExtractionStatus = s.ExtractionStatus,
                ExtractionMessage = s.ExtractionMessage,
                s.CreatedAt,
                Tags = s.Tags.Select(t => new { t.TagTypeId, TagType = t.TagType.Name, t.Value })
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
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagType)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagOption)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (source == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            source.Id,
            source.Name,
            source.Type,
            source.Date,
            source.Content,
            source.MetadataJson,
            source.ExternalId,
            IntegrationName = source.Integration.DisplayName,
            source.IsInternal,
            FragmentsCount = source.Fragments.Count,
            ExtractionStatus = source.ExtractionStatus,
            ExtractionMessage = source.ExtractionMessage,
            source.CreatedAt,
            Tags = source.Tags.Select(t => new
            {
                t.TagTypeId,
                TagType = t.TagType.Name,
                t.Value,
                AllowedValue = t.TagOption?.Value
            })
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
                s.Date,
                extractionStatus = s.ExtractionStatus
            })
            .ToListAsync();

        return Ok(sources);
    }

    /// <summary>
    /// Extract fragments from a source using AI
    /// </summary>
    [HttpPost("{id}/extract-fragments")]
    public async Task<IActionResult> ExtractFragments(Guid id)
    {
        try
        {
            _logger.LogInformation("Queueing fragment extraction for source {SourceId}", id);

            // Validate that the source exists
            var sourceExists = await _sourceRepository.Query()
                .AnyAsync(s => s.Id == id);

            if (!sourceExists)
            {
                _logger.LogWarning("Source {SourceId} not found", id);
                return NotFound(new
                {
                    success = false,
                    message = $"Source {id} not found"
                });
            }

            // Queue the background job
            var jobId = _backgroundJobService.Enqueue<FragmentExtractionJob>(
                job => job.ExecuteAsync(default!, default, id));

            _logger.LogInformation("Fragment extraction job {JobId} queued for source {SourceId}", jobId, id);

            return Ok(new
            {
                success = true,
                jobId,
                message = "Fragment extraction started. This may take a few minutes."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while queueing fragment extraction for source {SourceId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "An unexpected error occurred. Please try again."
            });
        }
    }

    /// <summary>
    /// Trigger smart tagging for a single source.
    /// </summary>
    [HttpPost("{id}/tag")]
    public async Task<IActionResult> TagSource(Guid id, [FromQuery] bool force = false)
    {
        var result = await _taggingService.GenerateTagsAsync(id, force);

        return Ok(new
        {
            success = result.Processed,
            skipped = !result.Processed,
            message = result.SkipReason ?? result.Message,
            isInternal = result.IsInternal,
            tagCount = result.TagCount
        });
    }
}

