using Hangfire;
using Medley.Application.Integrations.Models.Collector;
using Medley.Application.Interfaces;
using Medley.Application.Jobs;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Medley.Web.Controllers.Api;

[Route("api/sources")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class SourcesApiController : ControllerBase
{
    private readonly ISourceRepository _sourceRepository;
    private readonly IRepository<TagType> _tagTypeRepository;
    private readonly IRepository<Speaker> _speakerRepository;
    private readonly ITaggingService _taggingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<SourcesApiController> _logger;

    public SourcesApiController(
        ISourceRepository sourceRepository,
        IRepository<TagType> tagTypeRepository,
        IRepository<Speaker> speakerRepository,
        ITaggingService taggingService,
        IUnitOfWork unitOfWork,
        IBackgroundJobClient backgroundJobClient,
        ILogger<SourcesApiController> logger)
    {
        _sourceRepository = sourceRepository;
        _tagTypeRepository = tagTypeRepository;
        _speakerRepository = speakerRepository;
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
            .Include(s => s.Speakers)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (source == null)
        {
            return NotFound();
        }

        var dto = new SourceDto
        {
            Id = source.Id,
            Name = source.Name,
            Type = source.Type,
            MetadataType = source.MetadataType,
            Date = source.Date,
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
        };

        // For Fellow sources, parse speech segments
        if (source.MetadataType == SourceMetadataType.Collector_Fellow)
        {
            var (speechSegments, speakers) = ParseFellowSpeechSegments(source);
            dto.SpeechSegments = speechSegments;
            dto.Speakers = speakers;
        }
        else
        {
            // For other sources (like Google Drive), use the Content field
            dto.Content = source.Content;
        }

        return Ok(dto);
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

    /// <summary>
    /// Delete a source (soft delete)
    /// </summary>
    /// <param name="id">Source ID</param>
    /// <returns>Success or error message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteSourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteSourceResponse>> DeleteSource(Guid id)
    {
        try
        {
            // Use IgnoreQueryFilters to bypass the IsDeleted filter so we can find already deleted sources
            var source = await _sourceRepository.Query()
                .IgnoreQueryFilters()
                .Include(s => s.Fragments)
                    .ThenInclude(f => f.ClusteredInto)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (source == null)
            {
                return NotFound(new DeleteSourceResponse 
                { 
                    Success = false, 
                    Message = "Source not found" 
                });
            }

            if (source.IsDeleted)
            {
                return BadRequest(new DeleteSourceResponse 
                { 
                    Success = false, 
                    Message = "Source is already deleted" 
                });
            }

            // Check if any fragments have been merged/clustered into another fragment
            var clusteredFragments = source.Fragments
                .Where(f => !f.IsDeleted && f.ClusteredIntoId.HasValue)
                .ToList();

            if (clusteredFragments.Any())
            {
                var fragmentTitles = string.Join(", ", clusteredFragments.Take(3).Select(f => $"'{f.Title}'"));
                var moreCount = clusteredFragments.Count - 3;
                var message = clusteredFragments.Count == 1
                    ? $"Cannot delete source because fragment {fragmentTitles} has been clustered into another fragment."
                    : $"Cannot delete source because {clusteredFragments.Count} fragments ({fragmentTitles}{(moreCount > 0 ? $" and {moreCount} more" : "")}) have been clustered into other fragments.";

                return BadRequest(new DeleteSourceResponse
                {
                    Success = false,
                    Message = message
                });
            }

            // Delete the source (and cascade to fragments via soft delete)
            source.IsDeleted = true;
            
            // Also soft delete all fragments
            foreach (var fragment in source.Fragments.Where(f => !f.IsDeleted))
            {
                fragment.IsDeleted = true;
                fragment.LastModifiedAt = DateTimeOffset.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Source {SourceId} and {FragmentCount} fragments deleted", id, source.Fragments.Count(f => !f.IsDeleted));

            return Ok(new DeleteSourceResponse
            {
                Success = true,
                Message = $"Source and {source.Fragments.Count} fragment(s) deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting source {SourceId}", id);
            return StatusCode(500, new DeleteSourceResponse
            {
                Success = false,
                Message = "Failed to delete source. Please try again."
            });
        }
    }

    /// <summary>
    /// Parses Fellow metadata to extract speech segments and speakers
    /// </summary>
    private (List<SpeechSegmentDto>? speechSegments, List<SpeakerDto>? speakers) ParseFellowSpeechSegments(Source source)
    {
        try
        {
            var recording = JsonSerializer.Deserialize<FellowRecordingImportModel>(source.MetadataJson);
            
            if (recording?.Transcript?.SpeechSegments == null || recording.Transcript.SpeechSegments.Count == 0)
            {
                return (null, null);
            }

            // Get speaker entities for this source
            var speakerLookup = source.Speakers.ToDictionary(s => s.Name, s => s);

            var speechSegments = new List<SpeechSegmentDto>();
            var speakers = new List<SpeakerDto>();
            var seenSpeakers = new HashSet<Guid>();

            SpeechSegmentDto? currentSegment = null;

            foreach (var segment in recording.Transcript.SpeechSegments)
            {
                if (string.IsNullOrWhiteSpace(segment.Text))
                {
                    continue;
                }

                var cleanedSpeakerName = string.IsNullOrWhiteSpace(segment.Speaker) 
                    ? "Unknown Speaker" 
                    : RemoveSpeakerSuffixes(segment.Speaker);

                // Find the speaker entity
                Speaker? speaker = null;
                speakerLookup.TryGetValue(cleanedSpeakerName, out speaker);

                // Add speaker to the list if not already added
                if (speaker != null && !seenSpeakers.Contains(speaker.Id))
                {
                    speakers.Add(new SpeakerDto
                    {
                        Id = speaker.Id,
                        Name = speaker.Name,
                        Email = speaker.Email,
                        IsInternal = speaker.IsInternal,
                        TrustLevel = speaker.TrustLevel?.ToString()
                    });
                    seenSpeakers.Add(speaker.Id);
                }

                // Merge adjacent segments by the same speaker (based on cleaned name)
                if (currentSegment != null && currentSegment.SpeakerName == cleanedSpeakerName)
                {
                    currentSegment.Text += " " + segment.Text.Trim();
                }
                else
                {
                    // Start a new segment
                    if (currentSegment != null)
                    {
                        speechSegments.Add(currentSegment);
                    }

                    currentSegment = new SpeechSegmentDto
                    {
                        SpeakerId = speaker?.Id,
                        SpeakerName = cleanedSpeakerName,
                        Text = segment.Text.Trim()
                    };
                }
            }

            // Add the last segment
            if (currentSegment != null)
            {
                speechSegments.Add(currentSegment);
            }

            return speechSegments.Count > 0 ? (speechSegments, speakers) : (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Fellow speech segments for source {SourceId}", source.Id);
            return (null, null);
        }
    }

    /// <summary>
    /// Removes common suffixes from speaker names like (1), (2), - A, - B, (A), (B)
    /// </summary>
    private static string RemoveSpeakerSuffixes(string speakerName)
    {
        speakerName = speakerName.Trim();
        
        bool changed;
        do
        {
            changed = false;
            var original = speakerName;
            
            // Remove suffixes like (1), (2), (3), etc.
            speakerName = Regex.Replace(speakerName, @"\s*\(\d+\)$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
                continue;
            }
            
            // Remove suffixes like (A), (B), (C), etc.
            speakerName = Regex.Replace(speakerName, @"\s*\([A-Z]\)$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
                continue;
            }
            
            // Remove suffixes like - A, - B, - C, etc.
            speakerName = Regex.Replace(speakerName, @"\s*-\s*[A-Z]$", "").Trim();
            if (speakerName != original)
            {
                changed = true;
            }
        } while (changed);
        
        return speakerName;
    }
}

