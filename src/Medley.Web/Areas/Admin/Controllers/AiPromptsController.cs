using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class AiPromptsController : Controller
{
    public IActionResult Index()
    {
        return View("Spa");
    }
}

