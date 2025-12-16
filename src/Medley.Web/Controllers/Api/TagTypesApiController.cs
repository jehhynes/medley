using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers.Api;

[Route("api/tagtypes")]
[ApiController]
[Authorize]
public class TagTypesApiController : ControllerBase
{
    private readonly IRepository<TagType> _tagTypeRepository;

    public TagTypesApiController(IRepository<TagType> tagTypeRepository)
    {
        _tagTypeRepository = tagTypeRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tagTypes = await _tagTypeRepository.Query()
            .Include(t => t.AllowedValues)
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Prompt,
                t.IsConstrained,
                t.ScopeUpdateMode,
                AllowedValues = t.AllowedValues.Select(v => v.Value).ToList()
            })
            .ToListAsync();

        return Ok(tagTypes);
    }
}

