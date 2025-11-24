using Medley.Application.Interfaces;
using Medley.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers.Api;

[Route("api/fragments")]
[ApiController]
[Authorize]
public class FragmentsApiController : ControllerBase
{
    private readonly IRepository<Fragment> _fragmentRepository;

    public FragmentsApiController(IRepository<Fragment> fragmentRepository)
    {
        _fragmentRepository = fragmentRepository;
    }

    /// <summary>
    /// Get all fragments (stub implementation)
    /// </summary>
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            message = "Fragments API - Coming Soon",
            count = 0,
            items = new List<object>()
        });
    }

    /// <summary>
    /// Get a specific fragment by ID (stub implementation)
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult Get(Guid id)
    {
        return Ok(new
        {
            message = "Fragment detail - Coming Soon",
            id
        });
    }
}

