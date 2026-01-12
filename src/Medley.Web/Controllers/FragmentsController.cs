using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

[Authorize]
public class FragmentsController : Controller
{
    public IActionResult Index(Guid? id = null)
    {
        ViewData["Title"] = "Fragments";
        return View("Spa");
    }
}

