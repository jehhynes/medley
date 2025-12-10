using Medley.Web.Models;
using Xunit;
using System.Collections.Generic;

namespace Medley.Tests.Web.Models
{
    public class DashboardViewModelTests
    {
        [Fact]
        public void DashboardViewModel_InitializesWithDefaultValues()
        {
            // Act
            var model = new DashboardViewModel();

            // Assert
            Assert.Equal(string.Empty, model.UserName);
            Assert.NotNull(model.Metrics);
            Assert.Equal(0, model.Metrics.TotalSources);
        }

        [Fact]
        public void DashboardViewModel_CanSetProperties()
        {
            // Arrange
            var model = new DashboardViewModel();
            var metrics = new DashboardMetrics 
            { 
                TotalSources = 5,
                TotalFragments = 10,
                TotalArticles = 3
            };

            // Act
            model.UserName = "testuser";
            model.Metrics = metrics;

            // Assert
            Assert.Equal("testuser", model.UserName);
            Assert.Equal(metrics, model.Metrics);
            Assert.Equal(5, model.Metrics.TotalSources);
        }
    }

    public class DashboardMetricsTests
    {
        [Fact]
        public void DashboardMetrics_InitializesWithDefaultValues()
        {
            // Act
            var metrics = new DashboardMetrics();

            // Assert
            Assert.Equal(0, metrics.TotalSources);
            Assert.NotNull(metrics.SourcesByType);
            Assert.Empty(metrics.SourcesByType);
            Assert.NotNull(metrics.SourcesByIntegration);
            Assert.Empty(metrics.SourcesByIntegration);
        }
    }
}
