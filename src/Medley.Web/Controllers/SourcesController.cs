using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

[Authorize]
public class SourcesController : Controller
{
    public IActionResult Index(Guid? id = null)
    {
        ViewData["Title"] = "Sources";
        return View("Spa");
    }
}

