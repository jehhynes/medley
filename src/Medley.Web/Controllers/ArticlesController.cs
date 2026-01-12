using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Controllers;

[Authorize]
public class ArticlesController : Controller
{
    public IActionResult Index(Guid? id = null)
    {
        ViewData["Title"] = "Articles";
        return View("Spa");
    }
}

