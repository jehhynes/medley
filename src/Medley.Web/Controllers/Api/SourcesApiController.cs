using Hangfire;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/sources")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
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
    /// <param name="query">Text search query</param>
    /// <param name="tagTypeId">Filter by tag type ID</param>
    /// <param name="value">Filter by tag value</param>
    /// <param name="skip">Number of sources to skip</param>
    /// <param name="take">Number of sources to take</param>
    /// <returns>List of sources</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<SourceSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SourceSummaryDto>>> GetAll(
        [FromQuery] string? query = null, 
        [FromQuery] Guid? tagTypeId = null, 
        [FromQuery] string? value = null, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 100)
    {
        IQueryable<Source> queryable = _sourceRepository.Query()
            .Include(s => s.Integration)
            .Include(s => s.Fragments)
            .Include(s => s.PrimarySpeaker)
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
            queryable = queryable.Where(s => s.Tags.Any(t => t.TagTypeId == tagTypeId.Value && t.Value == value));
        }

        var sources = await queryable
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.Id) // Deterministic tiebreaker for pagination
            .Skip(skip)
            .Take(take)
            .Select(s => new SourceSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Type = s.Type,
                Date = s.Date,
                IntegrationName = s.Integration.Name,
                FragmentsCount = s.Fragments.Count,
                ExtractionStatus = s.ExtractionStatus,
                PrimarySpeakerName = s.PrimarySpeaker != null ? s.PrimarySpeaker.Name : null,
                PrimarySpeakerTrustLevel = s.PrimarySpeaker != null ? s.PrimarySpeaker.TrustLevel : null
            })
            .ToListAsync();

        return Ok(sources);
    }

    /// <summary>
    /// Get a specific source by ID
    /// </summary>
    /// <param name="id">Source ID</param>
    /// <returns>Source details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SourceDto>> Get(Guid id)
    {
        var source = await _sourceRepository.Query()
            .Include(s => s.Integration)
            .Include(s => s.Fragments)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagType)
            .Include(s => s.Tags)
                .ThenInclude(t => t.TagOption)
            .Include(s => s.PrimarySpeaker)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (source == null)
        {
            return NotFound();
        }

        return Ok(new SourceDto
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            MetadataType = source.MetadataType,
            Date = source.Date,
            Content = source.Content,
            MetadataJson = source.MetadataJson,
            ExternalId = source.ExternalId,
            IntegrationName = source.Integration.Name,
            IsInternal = source.IsInternal,
            FragmentsCount = source.Fragments.Count,
            ExtractionStatus = source.ExtractionStatus,
            ExtractionMessage = source.ExtractionMessage,
            CreatedAt = source.CreatedAt,
            TagsGenerated = source.TagsGenerated,
            PrimarySpeakerName = source.PrimarySpeaker?.Name,
            PrimarySpeakerTrustLevel = source.PrimarySpeaker?.TrustLevel,
            Tags = source.Tags.Select(t => new SourceTagDto
            {
                TagTypeId = t.TagTypeId,
                TagType = t.TagType.Name,
                Value = t.Value,
                AllowedValue = t.TagOption?.Value
            }).ToList()
        });
    }

    /// <summary>
    /// Extract fragments from a source using AI
    /// </summary>
    /// <param name="id">Source ID</param>
    /// <returns>Job status</returns>
    [HttpPost("{id}/extract-fragments")]
    [ProducesResponseType(typeof(FragmentExtractionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FragmentExtractionResponse>> ExtractFragments(Guid id)
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
                return NotFound(new FragmentExtractionResponse
                {
                    Success = false,
                    JobId = null,
                    Message = $"Source {id} not found"
                });
            }

            // Queue the background job
            var jobId = _backgroundJobClient.Enqueue<FragmentExtractionJob>(job => job.ExecuteAsync(id, default!, default));

            _logger.LogInformation("Fragment extraction job {JobId} queued for source {SourceId}", jobId, id);

            return Ok(new FragmentExtractionResponse
            {
                Success = true,
                JobId = jobId,
                Message = "Fragment extraction started. This may take a few minutes."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while queueing fragment extraction for source {SourceId}", id);
            return StatusCode(500, new FragmentExtractionResponse
            {
                Success = false,
                JobId = null,
                Message = "An unexpected error occurred. Please try again."
            });
        }
    }

    /// <summary>
    /// Trigger smart tagging for a single source
    /// </summary>
    /// <param name="id">Source ID</param>
    /// <param name="force">Force re-tagging even if already tagged</param>
    /// <returns>Tagging result</returns>
    [HttpPost("{id}/tag")]
    [ProducesResponseType(typeof(TaggingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TaggingResponse>> TagSource(Guid id, [FromQuery] bool force = false)
    {
        var result = await _taggingService.GenerateTagsAsync(id, force);

        return Ok(new TaggingResponse
        {
            Success = result.Processed,
            Skipped = !result.Processed,
            Message = result.SkipReason ?? result.Message ?? string.Empty,
            IsInternal = result.IsInternal,
            TagCount = result.TagCount
        });
    }
}

