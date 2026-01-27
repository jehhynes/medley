namespace Medley.Web.Models;

public class TokenUsageMetrics
{
    /// <summary>
    /// Daily token usage for the last 30 days (stacked: input, output, embedding)
    /// </summary>
    public List<DailyTokenUsage> DailyUsage { get; set; } = new();

    /// <summary>
    /// Token distribution by type (all time)
    /// </summary>
    public TokensByType AllTimeByType { get; set; } = new();

    /// <summary>
    /// Token distribution by type (last 30 days)
    /// </summary>
    public TokensByType Last30DaysByType { get; set; } = new();

    /// <summary>
    /// Token distribution by service name (all time)
    /// </summary>
    public List<MetricItem> AllTimeByService { get; set; } = new();

    /// <summary>
    /// Token distribution by service name (last 30 days)
    /// </summary>
    public List<MetricItem> Last30DaysByService { get; set; } = new();
}

public class DailyTokenUsage
{
    public string Date { get; set; } = string.Empty;
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long EmbeddingTokens { get; set; }
}

public class TokensByType
{
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long EmbeddingTokens { get; set; }
}
