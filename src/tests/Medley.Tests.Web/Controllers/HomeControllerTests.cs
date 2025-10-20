using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Medley.Web.Controllers;
using Medley.Web.Models;
using Medley.Domain.Entities;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Medley.Tests.Web.Controllers
{
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _mockLogger = new Mock<ILogger<HomeController>>();
            _mockUserManager = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
            
            _controller = new HomeController(_mockLogger.Object, _mockUserManager.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewWithDashboardViewModel()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@example.com")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.Equal("testuser@example.com", model.UserName);
            Assert.NotNull(model.SystemStatus);
            Assert.NotNull(model.RecentActivity);
            Assert.True(model.RecentActivity.Count > 0);
        }

        [Fact]
        public async Task Index_WithAnonymousUser_ReturnsGuestName()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.Equal("Guest", model.UserName);
        }

        [Fact]
        public async Task Index_SystemStatus_ContainsAllRequiredProperties()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@example.com")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.True(model.SystemStatus.DatabaseConnected);
            Assert.True(model.SystemStatus.AwsServicesActive);
            Assert.True(model.SystemStatus.BackgroundJobsRunning);
            Assert.True(model.SystemStatus.SecurityProtected);
        }

        [Fact]
        public async Task Index_RecentActivity_ContainsExpectedItems()
        {
            // Arrange
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser@example.com")
            }, "test"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<DashboardViewModel>(viewResult.Model);
            
            Assert.True(model.RecentActivity.Count >= 3);
            
            var systemInit = model.RecentActivity.FirstOrDefault(a => a.Title == "System initialized");
            Assert.NotNull(systemInit);
            Assert.Equal("Application started successfully", systemInit.Description);
            
            var dbConnection = model.RecentActivity.FirstOrDefault(a => a.Title == "Database connection established");
            Assert.NotNull(dbConnection);
            Assert.Equal("PostgreSQL with pgvector extension", dbConnection.Description);
            
            var authConfig = model.RecentActivity.FirstOrDefault(a => a.Title == "User authentication configured");
            Assert.NotNull(authConfig);
            Assert.Equal("ASP.NET Core Identity setup complete", authConfig.Description);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            // Act
            var result = _controller.Privacy();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.TraceIdentifier = "test-trace-id";
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("test-trace-id", model.RequestId);
        }
    }
}
