namespace Medley.Web.Models;

/// <summary>
/// Model information with token type capabilities
/// </summary>
public class ModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public bool HasInputTokens { get; set; }
    public bool HasOutputTokens { get; set; }
    public bool HasEmbeddingTokens { get; set; }
}

/// <summary>
/// Cost parameter for a specific AI model
/// </summary>
public class ModelCostParameter
{
    public Guid Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public decimal? InputCostPerMillion { get; set; }
    public decimal? OutputCostPerMillion { get; set; }
    public decimal? EmbeddingCostPerMillion { get; set; }
}

/// <summary>
/// Request to create or update cost parameters
/// </summary>
public class SaveCostParameterRequest
{
    public string ModelName { get; set; } = string.Empty;
    public decimal? InputCostPerMillion { get; set; }
    public decimal? OutputCostPerMillion { get; set; }
    public decimal? EmbeddingCostPerMillion { get; set; }
}

/// <summary>
/// Daily cost breakdown
/// </summary>
public class DailyCostEstimate
{
    public string Date { get; set; } = string.Empty;
    public decimal InputCost { get; set; }
    public decimal OutputCost { get; set; }
    public decimal EmbeddingCost { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// Cost estimation metrics
/// </summary>
public class CostEstimateMetrics
{
    public List<DailyCostEstimate> DailyCosts { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public List<string> MissingCostParameters { get; set; } = new();
}
