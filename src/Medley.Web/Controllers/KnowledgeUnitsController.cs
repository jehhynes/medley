using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

[Authorize]
public class KnowledgeUnitsController : Controller
{
    public IActionResult Index(Guid? id = null)
    {
        ViewData["Title"] = "Knowledge Units";
        return View("Spa");
    }
}
