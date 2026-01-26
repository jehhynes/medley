using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/speakers")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize(Roles = "Admin")]
public class SpeakersApiController : ControllerBase
{
    private readonly IRepository<Speaker> _speakerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SpeakersApiController> _logger;

    public SpeakersApiController(
        IRepository<Speaker> speakerRepository,
        IUnitOfWork unitOfWork,
        ILogger<SpeakersApiController> logger)
    {
        _speakerRepository = speakerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get all speakers with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SpeakerListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SpeakerListDto>>> GetAll(
        [FromQuery] bool? isInternal = null,
        [FromQuery] string? search = null)
    {
        IQueryable<Speaker> query = _speakerRepository.Query()
            .Include(s => s.Sources);

        // Filter by internal status
        if (isInternal.HasValue)
        {
            query = query.Where(s => s.IsInternal == isInternal.Value);
        }

        // Search by name or email
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchLower) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower)));
        }

        var speakers = await query
            .OrderBy(s => s.Name)
            .Select(s => new SpeakerListDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                IsInternal = s.IsInternal,
                TrustLevel = s.TrustLevel,
                SourceCount = s.Sources.Count,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(speakers);
    }

    /// <summary>
    /// Get a specific speaker by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SpeakerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpeakerDetailDto>> GetById(Guid id)
    {
        var speaker = await _speakerRepository.Query()
            .Where(s => s.Id == id)
            .Select(s => new SpeakerDetailDto
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                IsInternal = s.IsInternal,
                TrustLevel = s.TrustLevel,
                SourceCount = s.Sources.Count,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (speaker == null)
        {
            return NotFound(new { message = "Speaker not found" });
        }

        return Ok(speaker);
    }

    /// <summary>
    /// Update a speaker
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(SpeakerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SpeakerDetailDto>> Update(
        Guid id,
        [FromBody] UpdateSpeakerRequest request)
    {
        try
        {
            var speaker = await _speakerRepository.Query()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (speaker == null)
            {
                return NotFound(new { message = "Speaker not found" });
            }

            // Update only the editable fields
            speaker.IsInternal = request.IsInternal;
            speaker.TrustLevel = request.TrustLevel;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated speaker {SpeakerId}: IsInternal={IsInternal}, TrustLevel={TrustLevel}",
                id, request.IsInternal, request.TrustLevel);

            // Fetch the updated data with source count
            var dto = await _speakerRepository.Query()
                .Where(s => s.Id == id)
                .Select(s => new SpeakerDetailDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Email = s.Email,
                    IsInternal = s.IsInternal,
                    TrustLevel = s.TrustLevel,
                    SourceCount = s.Sources.Count,
                    CreatedAt = s.CreatedAt
                })
                .FirstAsync();

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update speaker {SpeakerId}", id);
            return StatusCode(500, new { message = "Failed to update speaker" });
        }
    }
}
