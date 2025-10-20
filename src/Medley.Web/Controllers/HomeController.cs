using System.Diagnostics;
using Medley.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Medley.Domain.Entities;

namespace Medley.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<User> _userManager;

        public HomeController(ILogger<HomeController> logger, UserManager<User> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                UserName = User.Identity?.Name ?? "Guest",
                SystemStatus = new SystemStatusViewModel
                {
                    DatabaseConnected = true, // TODO: Implement actual health check
                    AwsServicesActive = true, // TODO: Implement actual health check
                    BackgroundJobsRunning = true, // TODO: Implement actual health check
                    SecurityProtected = true // TODO: Implement actual health check
                },
                RecentActivity = new List<ActivityItemViewModel>
                {
                    new ActivityItemViewModel
                    {
                        Title = "System initialized",
                        Description = "Application started successfully",
                        Timestamp = DateTime.Now,
                        Icon = "cil-check-circle",
                        Color = "success"
                    },
                    new ActivityItemViewModel
                    {
                        Title = "Database connection established",
                        Description = "PostgreSQL with pgvector extension",
                        Timestamp = DateTime.Now.AddMinutes(-2),
                        Icon = "cil-database",
                        Color = "info"
                    },
                    new ActivityItemViewModel
                    {
                        Title = "User authentication configured",
                        Description = "ASP.NET Core Identity setup complete",
                        Timestamp = DateTime.Now.AddMinutes(-5),
                        Icon = "cil-shield-alt",
                        Color = "primary"
                    }
                }
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
