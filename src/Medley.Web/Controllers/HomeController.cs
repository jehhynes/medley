using System.Diagnostics;
using Medley.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Medley.Domain.Entities;
using Medley.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Medley.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IRepository<Tag> _tagRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IRepository<Tag> tagRepository)
        {
            _logger = logger;
            _tagRepository = tagRepository;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("load-balancer-health-check")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            // Simple database connectivity check
            var _ = _tagRepository.Query().Where(x => true == false).SingleOrDefault();

            return Content("healthy");
        }
    }
}
