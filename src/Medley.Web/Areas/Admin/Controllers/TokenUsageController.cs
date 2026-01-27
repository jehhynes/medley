using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class TokenUsageController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Token Usage";
        return View("Spa");
    }
}
