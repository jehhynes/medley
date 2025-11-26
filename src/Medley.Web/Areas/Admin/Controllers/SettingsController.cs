using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Medley.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

