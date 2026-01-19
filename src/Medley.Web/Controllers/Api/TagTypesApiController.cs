using Medley.Application.Interfaces;
using Medley.Application.Models.DTOs;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/tagtypes")]
[ApiController]
[ApiExplorerSettings(GroupName = "api")]
[Authorize]
public class TagTypesApiController : ControllerBase
{
    private readonly IRepository<TagType> _tagTypeRepository;

    public TagTypesApiController(IRepository<TagType> tagTypeRepository)
    {
        _tagTypeRepository = tagTypeRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TagTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TagTypeDto>>> GetAll()
    {
        var tagTypes = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .OrderBy(t => t.Name)
            .Select(t => new TagTypeDto
            {
                Id = t.Id,
                Name = t.Name,
                Prompt = t.Prompt,
                IsConstrained = t.IsConstrained,
                ScopeUpdateMode = t.ScopeUpdateMode,
                AllowedValues = t.AllowedValues.Select(v => v.Value).ToList()
            })
            .ToListAsync();

        return Ok(tagTypes);
    }
}

