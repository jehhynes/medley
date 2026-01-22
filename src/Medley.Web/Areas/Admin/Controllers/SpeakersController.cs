using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SpeakersController : Controller
{
    public IActionResult Index(Guid? id = null)
    {
        ViewData["Title"] = "Speakers";
        return View("Spa");
    }
}
