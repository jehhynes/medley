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
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IEmbeddingHelper _embeddingHelper;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly ILogger<FragmentsApiController> _logger;
    private readonly AiCallContext _aiCallContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMedleyContext _medleyContext;

    public FragmentsApiController(
        IFragmentRepository fragmentRepository,
        IRepository<Article> articleRepository,
        IRepository<Organization> organizationRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IEmbeddingHelper embeddingHelper,
        IOptions<EmbeddingSettings> embeddingSettings,
        ILogger<FragmentsApiController> logger,
        AiCallContext aiCallContext,
        IUnitOfWork unitOfWork,
        IMedleyContext medleyContext)
    {
        _fragmentRepository = fragmentRepository;
        _articleRepository = articleRepository;
        _organizationRepository = organizationRepository;
        _embeddingGenerator = embeddingGenerator;
        _embeddingHelper = embeddingHelper;
        _embeddingSettings = embeddingSettings.Value;
        _logger = logger;
        _aiCallContext = aiCallContext;
        _unitOfWork = unitOfWork;
        _medleyContext = medleyContext;
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
                .ThenInclude(s => s!.PrimarySpeaker)
            .Include(f => f.FragmentCategory)
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
                Category = f.FragmentCategory.Name,
                CategoryIcon = f.FragmentCategory.Icon,
                Content = f.Content,
                SourceId = f.Source == null ? null : (Guid?)f.Source.Id,
                SourceName = f.Source == null ? null : (string?)f.Source.Name,
                SourceType = f.Source == null ? null : (SourceType?)f.Source.Type,
                SourceDate = f.Source == null ? null : (DateTimeOffset?)f.Source.Date,
                PrimarySpeaker = f.Source == null || f.Source.PrimarySpeaker == null ? null : new SpeakerSummaryDto
                {
                    Id = f.Source.PrimarySpeaker.Id,
                    Name = f.Source.PrimarySpeaker.Name,
                    TrustLevel = f.Source.PrimarySpeaker.TrustLevel
                },
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
                .ThenInclude(s => s!.PrimarySpeaker)
            .Include(f => f.FragmentCategory)
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
            Category = fragment.FragmentCategory.Name,
            CategoryIcon = fragment.FragmentCategory.Icon,
            Content = fragment.Content,
            SourceId = fragment.Source == null ? null : (Guid?)fragment.Source.Id,
            SourceName = fragment.Source == null ? null : (string?)fragment.Source.Name,
            SourceType = fragment.Source == null ? null : (SourceType?)fragment.Source.Type,
            SourceDate = fragment.Source == null ? null : (DateTimeOffset?)fragment.Source.Date,
            PrimarySpeaker = fragment.Source == null || fragment.Source.PrimarySpeaker == null ? null : new SpeakerSummaryDto
            {
                Id = fragment.Source.PrimarySpeaker.Id,
                Name = fragment.Source.PrimarySpeaker.Name,
                TrustLevel = fragment.Source.PrimarySpeaker.TrustLevel
            },
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
                .ThenInclude(s => s!.PrimarySpeaker)
            .Include(f => f.FragmentCategory)
            .Where(f => f.Source!.Id == sourceId)
            .OrderByDescending(f => f.CreatedAt)
            .ThenBy(f => f.Id) // Deterministic tiebreaker
            .Select(f => MapToFragmentDto(f))
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Get all fragments for a specific article
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <returns>List of fragments linked to the article</returns>
    [HttpGet("by-article/{articleId}")]
    [ProducesResponseType(typeof(List<FragmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FragmentDto>>> GetByArticleId(Guid articleId)
    {
        // Get fragment IDs from the join table first
        var fragmentIds = await _articleRepository.Query()
            .Where(a => a.Id == articleId)
            .SelectMany(a => a.Fragments.Select(f => f.Id))
            .ToListAsync();

        // Now get the full fragment data
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
                .ThenInclude(s => s!.PrimarySpeaker)
            .Include(f => f.FragmentCategory)
            .Where(f => fragmentIds.Contains(f.Id))
            .OrderBy(f => f.Title)
            .ThenBy(f => f.Id) // Deterministic tiebreaker
            .Select(f => MapToFragmentDto(f))
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Get all fragments for a specific knowledge unit
    /// </summary>
    /// <param name="knowledgeUnitId">Knowledge unit ID</param>
    /// <returns>List of fragments linked to the knowledge unit</returns>
    [HttpGet("by-knowledge-unit/{knowledgeUnitId}")]
    [ProducesResponseType(typeof(List<FragmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FragmentDto>>> GetByKnowledgeUnitId(Guid knowledgeUnitId)
    {
        var fragments = await _fragmentRepository.Query()
            .Include(f => f.Source)
                .ThenInclude(s => s!.PrimarySpeaker)
            .Include(f => f.FragmentCategory)
            .Where(f => f.KnowledgeUnitId == knowledgeUnitId)
            .OrderBy(f => f.Title)
            .ThenBy(f => f.Id) // Deterministic tiebreaker
            .Select(f => MapToFragmentDto(f))
            .ToListAsync();

        return Ok(fragments);
    }

    /// <summary>
    /// Helper method to map Fragment entity to FragmentDto
    /// </summary>
    private static FragmentDto MapToFragmentDto(Fragment f)
    {
        return new FragmentDto
        {
            Id = f.Id,
            Title = f.Title,
            Summary = f.Summary,
            Category = f.FragmentCategory.Name,
            CategoryIcon = f.FragmentCategory.Icon,
            Content = f.Content,
            SourceId = f.Source == null ? null : (Guid?)f.Source.Id,
            SourceName = f.Source == null ? null : (string?)f.Source.Name,
            SourceType = f.Source == null ? null : (SourceType?)f.Source.Type,
            SourceDate = f.Source == null ? null : (DateTimeOffset?)f.Source.Date,
            PrimarySpeaker = f.Source == null || f.Source.PrimarySpeaker == null ? null : new SpeakerSummaryDto
            {
                Id = f.Source.PrimarySpeaker.Id,
                Name = f.Source.PrimarySpeaker.Name,
                TrustLevel = f.Source.PrimarySpeaker.TrustLevel
            },
            CreatedAt = f.CreatedAt,
            LastModifiedAt = f.LastModifiedAt,
            Confidence = f.Confidence,
            ConfidenceComment = f.ConfidenceComment
        };
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
                    .ThenInclude(s => s!.PrimarySpeaker)
                .Include(f => f.FragmentCategory)
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
                        Category = fragment.FragmentCategory.Name,
                        CategoryIcon = fragment.FragmentCategory.Icon,
                        SourceId = fragment.Source == null ? null : (Guid?)fragment.Source.Id,
                        SourceName = fragment.Source == null ? null : (string?)fragment.Source.Name,
                        SourceType = fragment.Source == null ? null : (SourceType?)fragment.Source.Type,
                        SourceDate = fragment.Source == null ? null : (DateTimeOffset?)fragment.Source.Date,
                        PrimarySpeaker = fragment.Source == null || fragment.Source.PrimarySpeaker == null ? null : new SpeakerSummaryDto
                        {
                            Id = fragment.Source.PrimarySpeaker.Id,
                            Name = fragment.Source.PrimarySpeaker.Name,
                            TrustLevel = fragment.Source.PrimarySpeaker.TrustLevel
                        },
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

    /// <summary>
    /// Update the confidence level of a fragment
    /// </summary>
    /// <param name="id">Fragment ID</param>
    /// <param name="request">Update request with new confidence level and optional comment</param>
    /// <returns>Updated fragment details</returns>
    [HttpPut("{id}/confidence")]
    [ProducesResponseType(typeof(FragmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FragmentDto>> UpdateConfidence(Guid id, [FromBody] UpdateFragmentConfidenceRequest request)
    {
        try
        {
            var fragment = await _fragmentRepository.Query()
                .Include(f => f.Source)
                    .ThenInclude(s => s!.PrimarySpeaker)
                .Include(f => f.FragmentCategory)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fragment == null)
            {
                return NotFound(new { message = "Fragment not found" });
            }

            // Get current user info
            var currentUser = await _medleyContext.GetCurrentUserAsync();
            var userName = currentUser?.FullName ?? "Unknown User";
            var oldValue = fragment.Confidence?.ToString() ?? "None";
            var newValue = request.Confidence?.ToString() ?? "None";
            var changeDate = DateTimeOffset.UtcNow;

            // Get organization timezone
            var organization = await _organizationRepository.Query().SingleAsync();
            var timeZoneId = organization.TimeZone;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localChangeDate = TimeZoneInfo.ConvertTime(changeDate, timeZoneInfo);

            // Build audit trail entry
            var auditEntry = $"\r\n - {userName} changed from {oldValue} to {newValue} on {localChangeDate:yyyy-MM-dd HH:mm:ss}";

            // Update confidence level
            fragment.Confidence = request.Confidence;

            // Append audit trail to confidence comment
            if (!string.IsNullOrWhiteSpace(request.ConfidenceComment))
            {
                fragment.ConfidenceComment = request.ConfidenceComment + auditEntry;
            }
            else if (!string.IsNullOrWhiteSpace(fragment.ConfidenceComment))
            {
                fragment.ConfidenceComment += auditEntry;
            }
            else
            {
                fragment.ConfidenceComment = auditEntry.TrimStart('\r', '\n');
            }

            // Update last modified timestamp
            fragment.LastModifiedAt = changeDate;

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Fragment {FragmentId} confidence updated from {OldValue} to {NewValue} by user {UserId}",
                id, oldValue, newValue, currentUser?.Id);

            // Return updated fragment
            return Ok(new FragmentDto
            {
                Id = fragment.Id,
                Title = fragment.Title,
                Summary = fragment.Summary,
                Category = fragment.FragmentCategory.Name,
                CategoryIcon = fragment.FragmentCategory.Icon,
                Content = fragment.Content,
                SourceId = fragment.Source == null ? null : (Guid?)fragment.Source.Id,
                SourceName = fragment.Source == null ? null : (string?)fragment.Source.Name,
                SourceType = fragment.Source == null ? null : (SourceType?)fragment.Source.Type,
                SourceDate = fragment.Source == null ? null : (DateTimeOffset?)fragment.Source.Date,
                PrimarySpeaker = fragment.Source == null || fragment.Source.PrimarySpeaker == null ? null : new SpeakerSummaryDto
                {
                    Id = fragment.Source.PrimarySpeaker.Id,
                    Name = fragment.Source.PrimarySpeaker.Name,
                    TrustLevel = fragment.Source.PrimarySpeaker.TrustLevel
                },
                CreatedAt = fragment.CreatedAt,
                LastModifiedAt = fragment.LastModifiedAt,
                Confidence = fragment.Confidence,
                ConfidenceComment = fragment.ConfidenceComment
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fragment confidence for fragment {FragmentId}", id);
            return StatusCode(500, new { message = "Failed to update fragment confidence. Please try again." });
        }
    }

    /// <summary>
    /// Delete a fragment (soft delete)
    /// </summary>
    /// <param name="id">Fragment ID</param>
    /// <returns>Success or error message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteFragmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteFragmentResponse>> DeleteFragment(Guid id)
    {
        try
        {
            // Use IgnoreQueryFilters to bypass the IsDeleted filter so we can find already deleted fragments
            var fragment = await _fragmentRepository.Query()
                .IgnoreQueryFilters()
                .Include(f => f.KnowledgeUnit)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (fragment == null)
            {
                return NotFound(new DeleteFragmentResponse 
                { 
                    Success = false, 
                    Message = "Fragment not found" 
                });
            }

            if (fragment.IsDeleted)
            {
                return BadRequest(new DeleteFragmentResponse 
                { 
                    Success = false, 
                    Message = "Fragment is already deleted" 
                });
            }

            // Check if fragment has been merged/clustered into a KnowledgeUnit
            if (fragment.KnowledgeUnitId.HasValue)
            {
                var knowledgeUnitTitle = fragment.KnowledgeUnit?.Title ?? "a knowledge unit";
                return BadRequest(new DeleteFragmentResponse
                {
                    Success = false,
                    Message = $"Cannot delete fragment because it has been clustered into '{knowledgeUnitTitle}'."
                });
            }

            // Delete the fragment
            fragment.IsDeleted = true;
            fragment.LastModifiedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Fragment {FragmentId} deleted", id);

            return Ok(new DeleteFragmentResponse
            {
                Success = true,
                Message = "Fragment deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fragment {FragmentId}", id);
            return StatusCode(500, new DeleteFragmentResponse
            {
                Success = false,
                Message = "Failed to delete fragment. Please try again."
            });
        }
    }
}
