using System.ComponentModel.DataAnnotations;

namespace Medley.Web.Models
{
    public class DashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public DashboardMetrics Metrics { get; set; } = new();
    }

    public class DashboardMetrics
    {
        // Source metrics
        public int TotalSources { get; set; }
        public List<MetricItem> SourcesByType { get; set; } = new();
        public List<MetricItem> SourcesByIntegration { get; set; } = new();
        public List<StackedMetricItem> SourcesByYear { get; set; } = new();
        public List<MetricItem> SourcesByMonth { get; set; } = new();
        public List<TagTypeMetrics> SourcesByTagType { get; set; } = new();
        public int SourcesPendingSmartTagging { get; set; }
        public int SourcesPendingFragmentGeneration { get; set; }

        // Fragment metrics
        public int TotalFragments { get; set; }
        public List<MetricItem> FragmentsByCategory { get; set; } = new();
        public int FragmentsPendingEmbedding { get; set; }
        public int FragmentsPendingKnowledgeUnitGeneration { get; set; }

        // Knowledge Unit metrics
        public int TotalKnowledgeUnits { get; set; }
        public List<MetricItem> KnowledgeUnitsByCategory { get; set; } = new();
        public int KnowledgeUnitsPendingEmbedding { get; set; }

        // Article metrics
        public int TotalArticles { get; set; }
        public List<MetricItem> ArticlesByType { get; set; } = new();
    }

    public class TagTypeMetrics
    {
        public string TagTypeName { get; set; } = string.Empty;
        public List<MetricItem> TagCounts { get; set; } = new();
    }

    public class MetricItem
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? Icon { get; set; }
    }

    public class StackedMetricItem
    {
        public string Label { get; set; } = string.Empty;
        public Dictionary<string, int> Values { get; set; } = new();
    }
}
