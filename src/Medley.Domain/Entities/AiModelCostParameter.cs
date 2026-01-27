using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Stores cost parameters for AI models to estimate token usage costs
/// System-wide configuration (not per organization)
/// </summary>
[Index(nameof(ModelName), IsUnique = true, Name = "IX_AiModelCostParameter_ModelName")]
public class AiModelCostParameter : BaseEntity
{
    /// <summary>
    /// The AI model identifier (e.g., "anthropic.claude-3-5-sonnet-20241022-v2:0", "mxbai-embed-large")
    /// Must match ModelName from AiTokenUsage
    /// </summary>
    [MaxLength(200)]
    public required string ModelName { get; set; }

    /// <summary>
    /// Cost per million input tokens (null if model doesn't use input tokens)
    /// </summary>
    public decimal? InputCostPerMillion { get; set; }

    /// <summary>
    /// Cost per million output tokens (null if model doesn't use output tokens)
    /// </summary>
    public decimal? OutputCostPerMillion { get; set; }

    /// <summary>
    /// Cost per million embedding tokens (null if model doesn't use embedding tokens)
    /// </summary>
    public decimal? EmbeddingCostPerMillion { get; set; }
}
