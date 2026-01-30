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

[Route("api/knowledge-units")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class KnowledgeUnitsApiController : ControllerBase
{
    private readonly IKnowledgeUnitRepository _knowledgeUnitRepository;
    private readonly IRepository<Organization> _organizationRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly IEmbeddingHelper _embeddingHelper;
    private readonly EmbeddingSettings _embeddingSettings;
    private readonly ILogger<KnowledgeUnitsApiController> _logger;
    private readonly AiCallContext _aiCallContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMedleyContext _medleyContext;

    public KnowledgeUnitsApiController(
        IKnowledgeUnitRepository knowledgeUnitRepository,
        IRepository<Organization> organizationRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IEmbeddingHelper embeddingHelper,
        IOptions<EmbeddingSettings> embeddingSettings,
        ILogger<KnowledgeUnitsApiController> logger,
        AiCallContext aiCallContext,
        IUnitOfWork unitOfWork,
        IMedleyContext medleyContext)
    {
        _knowledgeUnitRepository = knowledgeUnitRepository;
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
    /// Get all knowledge units with pagination
    /// </summary>
    /// <param name="skip">Number of knowledge units to skip</param>
    /// <param name="take">Number of knowledge units to take</param>
    /// <returns>List of knowledge units</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<KnowledgeUnitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeUnitDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var knowledgeUnits = await _knowledgeUnitRepository.Query()
            .Include(ku => ku.Category)
            .Include(ku => ku.FragmentKnowledgeUnits)
                .ThenInclude(fku => fku.Fragment)
            .OrderByDescending(ku => ku.UpdatedAt)
            .ThenBy(ku => ku.Id) // Deterministic tiebreaker for pagination
            .Skip(skip)
            .Take(take)
            .Select(ku => MapToKnowledgeUnitDto(ku))
            .ToListAsync();

        return Ok(knowledgeUnits);
    }

    /// <summary>
    /// Get a specific knowledge unit by ID
    /// </summary>
    /// <param name="id">Knowledge unit ID</param>
    /// <returns>Knowledge unit details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(KnowledgeUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KnowledgeUnitDto>> Get(Guid id)
    {
        var knowledgeUnit = await _knowledgeUnitRepository.Query()
            .Include(ku => ku.Category)
            .Include(ku => ku.FragmentKnowledgeUnits)
                .ThenInclude(fku => fku.Fragment)
            .FirstOrDefaultAsync(ku => ku.Id == id);

        if (knowledgeUnit == null)
        {
            return NotFound();
        }

        return Ok(MapToKnowledgeUnitDto(knowledgeUnit));
    }

    /// <summary>
    /// Get titles for multiple knowledge units by their IDs
    /// </summary>
    /// <param name="ids">List of knowledge unit IDs</param>
    /// <returns>List of knowledge unit titles</returns>
    [HttpPost("titles")]
    [ProducesResponseType(typeof(List<KnowledgeUnitTitleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeUnitTitleDto>>> GetTitles([FromBody] List<Guid> ids)
    {
        var knowledgeUnits = await _knowledgeUnitRepository.Query()
            .Where(ku => ids.Contains(ku.Id))
            .Select(ku => new KnowledgeUnitTitleDto
            {
                Id = ku.Id,
                Title = ku.Title
            })
            .ToListAsync();

        return Ok(knowledgeUnits);
    }

    /// <summary>
    /// Get all knowledge units for a specific article
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <returns>List of knowledge units linked to the article</returns>
    [HttpGet("by-article/{articleId}")]
    [ProducesResponseType(typeof(List<KnowledgeUnitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KnowledgeUnitDto>>> GetByArticleId(Guid articleId)
    {
        var knowledgeUnits = await _knowledgeUnitRepository.Query()
            .Include(ku => ku.Category)
            .Include(ku => ku.FragmentKnowledgeUnits)
                .ThenInclude(fku => fku.Fragment)
            .Where(ku => ku.Articles.Any(a => a.Id == articleId))
            .OrderBy(ku => ku.Title)
            .ThenBy(ku => ku.Id) // Deterministic tiebreaker
            .Select(ku => MapToKnowledgeUnitDto(ku))
            .ToListAsync();

        return Ok(knowledgeUnits);
    }

    /// <summary>
    /// Search knowledge units using semantic similarity (vector search)
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="take">Number of results to return</param>
    /// <returns>List of knowledge units with similarity scores</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<KnowledgeUnitSearchResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<KnowledgeUnitSearchResult>>> Search([FromQuery] string query, [FromQuery] int take = 50)
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
            using (_aiCallContext.SetContext(nameof(KnowledgeUnitsApiController), nameof(Search), null, null))
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
            
            // Find similar knowledge units using vector similarity
            var similarKnowledgeUnits = await _knowledgeUnitRepository.FindSimilarAsync(
                processedVector, 
                take);

            // Load related data and map to response
            var knowledgeUnitIds = similarKnowledgeUnits.Select(r => r.KnowledgeUnit.Id).ToList();
            
            var knowledgeUnitsWithCategory = await _knowledgeUnitRepository.Query()
                .Include(ku => ku.Category)
                .Include(ku => ku.FragmentKnowledgeUnits)
                    .ThenInclude(fku => fku.Fragment)
                .Where(ku => knowledgeUnitIds.Contains(ku.Id))
                .ToListAsync();

            // Maintain the similarity order from the vector search
            var knowledgeUnitLookup = knowledgeUnitsWithCategory.ToDictionary(ku => ku.Id);
            var results = similarKnowledgeUnits
                .Where(r => knowledgeUnitLookup.ContainsKey(r.KnowledgeUnit.Id))
                .Select(result =>
                {
                    var knowledgeUnit = knowledgeUnitLookup[result.KnowledgeUnit.Id];
                    return new KnowledgeUnitSearchResult
                    {
                        Id = knowledgeUnit.Id,
                        Title = knowledgeUnit.Title,
                        Summary = knowledgeUnit.Summary,
                        Category = knowledgeUnit.Category.Name,
                        CategoryIcon = knowledgeUnit.Category.Icon,
                        Confidence = knowledgeUnit.Confidence,
                        ConfidenceComment = knowledgeUnit.ConfidenceComment,
                        FragmentCount = knowledgeUnit.FragmentKnowledgeUnits.Count,
                        UpdatedAt = knowledgeUnit.UpdatedAt,
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
    /// Update the confidence level of a knowledge unit
    /// </summary>
    /// <param name="id">Knowledge unit ID</param>
    /// <param name="request">Update request with new confidence level and optional comment</param>
    /// <returns>Updated knowledge unit details</returns>
    [HttpPut("{id}/confidence")]
    [ProducesResponseType(typeof(KnowledgeUnitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<KnowledgeUnitDto>> UpdateConfidence(Guid id, [FromBody] UpdateKnowledgeUnitConfidenceRequest request)
    {
        try
        {
            var knowledgeUnit = await _knowledgeUnitRepository.Query()
                .Include(ku => ku.Category)
                .Include(ku => ku.FragmentKnowledgeUnits)
                    .ThenInclude(fku => fku.Fragment)
                .FirstOrDefaultAsync(ku => ku.Id == id);

            if (knowledgeUnit == null)
            {
                return NotFound(new { message = "Knowledge unit not found" });
            }

            // Get current user info
            var currentUser = await _medleyContext.GetCurrentUserAsync();
            var userName = currentUser?.FullName ?? "Unknown User";
            var oldValue = knowledgeUnit.Confidence.ToString();
            var newValue = request.Confidence.ToString();
            var changeDate = DateTimeOffset.UtcNow;

            // Get organization timezone
            var organization = await _organizationRepository.Query().SingleAsync();
            var timeZoneId = organization.TimeZone;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localChangeDate = TimeZoneInfo.ConvertTime(changeDate, timeZoneInfo);

            // Build audit trail entry
            var auditEntry = $"\r\n - {userName} changed from {oldValue} to {newValue} on {localChangeDate:yyyy-MM-dd HH:mm:ss}";

            // Update confidence level
            knowledgeUnit.Confidence = request.Confidence;

            // Append audit trail to confidence comment
            if (!string.IsNullOrWhiteSpace(request.ConfidenceComment))
            {
                knowledgeUnit.ConfidenceComment = request.ConfidenceComment + auditEntry;
            }
            else if (!string.IsNullOrWhiteSpace(knowledgeUnit.ConfidenceComment))
            {
                knowledgeUnit.ConfidenceComment += auditEntry;
            }
            else
            {
                knowledgeUnit.ConfidenceComment = auditEntry.TrimStart('\r', '\n');
            }

            // Update timestamp
            knowledgeUnit.UpdatedAt = changeDate;

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Knowledge unit {KnowledgeUnitId} confidence updated from {OldValue} to {NewValue} by user {UserId}",
                id, oldValue, newValue, currentUser?.Id);

            // Return updated knowledge unit
            return Ok(MapToKnowledgeUnitDto(knowledgeUnit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating knowledge unit confidence for knowledge unit {KnowledgeUnitId}", id);
            return StatusCode(500, new { message = "Failed to update knowledge unit confidence. Please try again." });
        }
    }

    /// <summary>
    /// Delete a knowledge unit (soft delete)
    /// </summary>
    /// <param name="id">Knowledge unit ID</param>
    /// <returns>Success or error message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteKnowledgeUnitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeleteKnowledgeUnitResponse>> DeleteKnowledgeUnit(Guid id)
    {
        try
        {
            // Use IgnoreQueryFilters to bypass the IsDeleted filter so we can find already deleted knowledge units
            var knowledgeUnit = await _knowledgeUnitRepository.Query()
                .IgnoreQueryFilters()
                .Include(ku => ku.Articles)
                .FirstOrDefaultAsync(ku => ku.Id == id);

            if (knowledgeUnit == null)
            {
                return NotFound(new DeleteKnowledgeUnitResponse 
                { 
                    Success = false, 
                    Message = "Knowledge unit not found" 
                });
            }

            if (knowledgeUnit.IsDeleted)
            {
                return BadRequest(new DeleteKnowledgeUnitResponse 
                { 
                    Success = false, 
                    Message = "Knowledge unit is already deleted" 
                });
            }

            // Check if knowledge unit is referenced by any articles
            if (knowledgeUnit.Articles.Any())
            {
                return BadRequest(new DeleteKnowledgeUnitResponse
                {
                    Success = false,
                    Message = $"Cannot delete knowledge unit because it is referenced by {knowledgeUnit.Articles.Count} article(s)."
                });
            }

            // Soft delete the knowledge unit
            knowledgeUnit.IsDeleted = true;
            knowledgeUnit.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Knowledge unit {KnowledgeUnitId} soft deleted", id);

            return Ok(new DeleteKnowledgeUnitResponse
            {
                Success = true,
                Message = "Knowledge unit deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting knowledge unit {KnowledgeUnitId}", id);
            return StatusCode(500, new DeleteKnowledgeUnitResponse
            {
                Success = false,
                Message = "Failed to delete knowledge unit. Please try again."
            });
        }
    }

    /// <summary>
    /// Helper method to map KnowledgeUnit entity to KnowledgeUnitDto
    /// </summary>
    private static KnowledgeUnitDto MapToKnowledgeUnitDto(KnowledgeUnit ku)
    {
        return new KnowledgeUnitDto
        {
            Id = ku.Id,
            Title = ku.Title,
            Summary = ku.Summary,
            Content = ku.Content,
            Category = ku.Category.Name,
            CategoryIcon = ku.Category.Icon,
            Confidence = ku.Confidence,
            ConfidenceComment = ku.ConfidenceComment,
            ClusteringComment = ku.ClusteringComment,
            FragmentCount = ku.FragmentKnowledgeUnits.Count,
            CreatedAt = ku.CreatedAt,
            UpdatedAt = ku.UpdatedAt
        };
    }
}
