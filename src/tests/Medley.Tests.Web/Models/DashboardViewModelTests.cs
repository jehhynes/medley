using Medley.Web.Models;
using System;

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
            Assert.NotNull(model.SystemStatus);
            Assert.NotNull(model.RecentActivity);
            Assert.Empty(model.RecentActivity);
        }

        [Fact]
        public void DashboardViewModel_CanSetProperties()
        {
            // Arrange
            var model = new DashboardViewModel();
            var systemStatus = new SystemStatusViewModel();
            var activities = new List<ActivityItemViewModel>();

            // Act
            model.UserName = "testuser";
            model.SystemStatus = systemStatus;
            model.RecentActivity = activities;

            // Assert
            Assert.Equal("testuser", model.UserName);
            Assert.Equal(systemStatus, model.SystemStatus);
            Assert.Equal(activities, model.RecentActivity);
        }
    }

    public class SystemStatusViewModelTests
    {
        [Fact]
        public void SystemStatusViewModel_InitializesWithDefaultValues()
        {
            // Act
            var model = new SystemStatusViewModel();

            // Assert
            Assert.False(model.DatabaseConnected);
            Assert.False(model.AwsServicesActive);
            Assert.False(model.BackgroundJobsRunning);
            Assert.False(model.SecurityProtected);
        }

        [Fact]
        public void SystemStatusViewModel_CanSetProperties()
        {
            // Arrange
            var model = new SystemStatusViewModel();

            // Act
            model.DatabaseConnected = true;
            model.AwsServicesActive = true;
            model.BackgroundJobsRunning = true;
            model.SecurityProtected = true;

            // Assert
            Assert.True(model.DatabaseConnected);
            Assert.True(model.AwsServicesActive);
            Assert.True(model.BackgroundJobsRunning);
            Assert.True(model.SecurityProtected);
        }
    }

    public class ActivityItemViewModelTests
    {
        [Fact]
        public void ActivityItemViewModel_InitializesWithDefaultValues()
        {
            // Act
            var model = new ActivityItemViewModel();

            // Assert
            Assert.Equal(string.Empty, model.Title);
            Assert.Equal(string.Empty, model.Description);
            Assert.Equal(DateTime.MinValue, model.Timestamp);
            Assert.Equal(string.Empty, model.Icon);
            Assert.Equal(string.Empty, model.Color);
        }

        [Fact]
        public void ActivityItemViewModel_CanSetProperties()
        {
            // Arrange
            var model = new ActivityItemViewModel();
            var timestamp = DateTime.Now;

            // Act
            model.Title = "Test Activity";
            model.Description = "Test Description";
            model.Timestamp = timestamp;
            model.Icon = "cil-test";
            model.Color = "primary";

            // Assert
            Assert.Equal("Test Activity", model.Title);
            Assert.Equal("Test Description", model.Description);
            Assert.Equal(timestamp, model.Timestamp);
            Assert.Equal("cil-test", model.Icon);
            Assert.Equal("primary", model.Color);
        }

        [Fact]
        public void ActivityItemViewModel_TimeAgo_ReturnsJustNow_ForRecentTimestamp()
        {
            // Arrange
            var model = new ActivityItemViewModel
            {
                Timestamp = DateTime.Now.AddSeconds(-30)
            };

            // Act
            var timeAgo = model.TimeAgo;

            // Assert
            Assert.Equal("Just now", timeAgo);
        }

        [Fact]
        public void ActivityItemViewModel_TimeAgo_ReturnsMinutesAgo_ForMinutesOld()
        {
            // Arrange
            var model = new ActivityItemViewModel
            {
                Timestamp = DateTime.Now.AddMinutes(-5)
            };

            // Act
            var timeAgo = model.TimeAgo;

            // Assert
            Assert.Equal("5 minutes ago", timeAgo);
        }

        [Fact]
        public void ActivityItemViewModel_TimeAgo_ReturnsHoursAgo_ForHoursOld()
        {
            // Arrange
            var model = new ActivityItemViewModel
            {
                Timestamp = DateTime.Now.AddHours(-3)
            };

            // Act
            var timeAgo = model.TimeAgo;

            // Assert
            Assert.Equal("3 hours ago", timeAgo);
        }

        [Fact]
        public void ActivityItemViewModel_TimeAgo_ReturnsDaysAgo_ForDaysOld()
        {
            // Arrange
            var model = new ActivityItemViewModel
            {
                Timestamp = DateTime.Now.AddDays(-2)
            };

            // Act
            var timeAgo = model.TimeAgo;

            // Assert
            Assert.Equal("2 days ago", timeAgo);
        }
    }
}
