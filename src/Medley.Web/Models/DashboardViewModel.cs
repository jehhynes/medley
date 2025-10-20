using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public SystemStatusViewModel SystemStatus { get; set; } = new();
        public List<ActivityItemViewModel> RecentActivity { get; set; } = new();
    }

    public class SystemStatusViewModel
    {
        public bool DatabaseConnected { get; set; }
        public bool AwsServicesActive { get; set; }
        public bool BackgroundJobsRunning { get; set; }
        public bool SecurityProtected { get; set; }
    }

    public class ActivityItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        
        public string TimeAgo => GetTimeAgo(Timestamp);
        
        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            return $"{(int)timeSpan.TotalDays} days ago";
        }
    }
}
