using Hangfire;
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
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SourcesApiController> _logger;

    public SourcesApiController(
        IRepository<Source> sourceRepository,
        IRepository<TagType> tagTypeRepository,
        ITaggingService taggingService,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<SourcesApiController> logger)
    {
        _sourceRepository = sourceRepository;
        _tagTypeRepository = tagTypeRepository;
        _taggingService = taggingService;
        _unitOfWork = unitOfWork;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all sources, optionally filtered by text search and/or tag
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? query = null, 
        [FromQuery] Guid? tagTypeId = null, 
        [FromQuery] string? value = null, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 100)
    {
        IQueryable<Source> queryable = _sourceRepository.Query()
            .Include(s => s.Integration)
            .Include(s => s.Fragments)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagType);

        // Apply text search filter if provided
        if (!string.IsNullOrWhiteSpace(query))
        {
            queryable = queryable.Where(s => s.Name != null && s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        // Apply tag filter if provided
        if (tagTypeId.HasValue && tagTypeId.Value != Guid.Empty && !string.IsNullOrWhiteSpace(value))
        {
            queryable = queryable.Where(s => s.Tags.Any(t => t.TagType.Id == tagTypeId.Value && t.Value == value));
        }

        var sources = await queryable
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Id) // Deterministic tiebreaker for pagination
            .Skip(skip)
            .Take(take)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Type,
                s.Date,
                s.ExternalId,
                IntegrationName = s.Integration.Name,
                s.IsInternal,
                FragmentsCount = s.Fragments.Count,
                ExtractionStatus = s.ExtractionStatus,
                ExtractionMessage = s.ExtractionMessage,
                s.CreatedAt,
                s.TagsGenerated,
                Tags = s.Tags.Select(t => new { TagTypeId = t.TagType.Id, TagType = t.TagType.Name, t.Value })
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
            IntegrationName = source.Integration.Name,
            source.IsInternal,
            FragmentsCount = source.Fragments.Count,
            ExtractionStatus = source.ExtractionStatus,
            ExtractionMessage = source.ExtractionMessage,
            source.CreatedAt,
            source.TagsGenerated,
            Tags = source.Tags.Select(t => new
            {
                TagTypeId = t.TagType.Id,
                TagType = t.TagType.Name,
                t.Value,
                AllowedValue = t.TagOption?.Value
            })
        });
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
            var jobId = _backgroundJobClient.Enqueue<FragmentExtractionJob>(job => job.ExecuteAsync(id, default!, default));

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

