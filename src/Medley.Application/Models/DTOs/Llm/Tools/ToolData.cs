using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs.Llm.Tools;

/// <summary>
/// Request for creating an article improvement plan
/// </summary>
public class CreatePlanRequest
{
    /// <summary>
    /// Overall instructions for how to improve the article using the recommended knowledge units
    /// </summary>
    [Description("Overall instructions for how to improve the article using the recommended knowledge units in markdown format")]
    [Required]
    public required string Instructions { get; set; }

    /// <summary>
    /// Array of knowledge unit recommendations
    /// </summary>
    [Description("Array of knowledge unit recommendations")]
    [Required]
    [MinLength(1)]
    public required PlanKnowledgeUnitRecommendation[] Recommendations { get; set; }

    /// <summary>
    /// Optional summary of changes if this is a modification of an existing plan
    /// </summary>
    [Description("Optional summary of changes if this is a modification of an existing plan")]
    [MaxLength(500)]
    public string? ChangesSummary { get; set; }
}

/// <summary>
/// Represents a knowledge unit recommendation for a plan
/// </summary>
public class PlanKnowledgeUnitRecommendation
{
    [Description("The unique identifier of the knowledge unit being recommended")]
    public Guid KnowledgeUnitId { get; set; }

    [Description("The similarity score between the knowledge unit and the article (0.0 to 1.0)")]
    public double SimilarityScore { get; set; }

    [Description("Whether to include this knowledge unit in the article improvement (true) or exclude it (false)")]
    public bool Include { get; set; }

    [Description("Explanation of why this knowledge unit should not be included in the article. Omit if Include=true")]
    [MaxLength(200)] //Intentionally shorter to encourage brevity
    public string? Reasoning { get; set; }

    [Description("Optional instructions on how to incorporate this knowledge unit into the article")]
    [MaxLength(200)] //Intentionally shorter to encourage brevity
    public string? Instructions { get; set; }
}

public class CreateArticleVersionRequest
{
    /// <summary>
    /// The complete improved article content
    /// </summary>
    [Description("The complete improved article content")]
    [Required]
    public required string Content { get; set; }

    /// <summary>
    /// Description of changes made in this version
    /// </summary>
    [Description("Description of changes made in this version")]
    [Required]
    [MaxLength(200)] //Intentionally shorter to encourage brevity
    public required string ChangeMessage { get; set; }
}

/// <summary>
/// Knowledge unit search result with similarity score (no content)
/// </summary>
public class ToolKnowledgeUnitSearchResult
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public double SimilarityScore { get; set; }
    public ConfidenceLevel? Confidence { get; set; }
    public string? ConfidenceComment { get; set; }
}

/// <summary>
/// Knowledge unit data with content included
/// </summary>
public class KnowledgeUnitWithContentData
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Summary { get; set; }
    public string? Category { get; set; }
    public required string Content { get; set; }
    public ConfidenceLevel? Confidence { get; set; }
    public string? ConfidenceComment { get; set; }
    public SourceData? Source { get; set; }
}

/// <summary>
/// Response for SearchKnowledgeUnitsAsync
/// </summary>
public class SearchKnowledgeUnitsResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public List<ToolKnowledgeUnitSearchResult> KnowledgeUnits { get; set; } = new();
}

/// <summary>
/// Response for FindSimilarKnowledgeUnitsAsync
/// </summary>
public class FindSimilarKnowledgeUnitsResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public List<ToolKnowledgeUnitSearchResult> KnowledgeUnits { get; set; } = new();
}

/// <summary>
/// Response for GetKnowledgeUnitContentAsync
/// </summary>
public class GetKnowledgeUnitResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public KnowledgeUnitWithContentData? KnowledgeUnit { get; set; }
}

/// <summary>
/// Response for CreatePlanAsync
/// </summary>
public class CreatePlanResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public Guid? PlanId { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for CreateArticleVersionAsync
/// </summary>
public class CreateArticleVersionResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public Guid? VersionId { get; set; }
    public string? VersionNumber { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for ReviewArticleWithCursorAsync
/// </summary>
public class ReviewArticleWithCursorResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public Guid? VersionId { get; set; }
    public string? VersionNumber { get; set; }
    public string? ImprovedContent { get; set; }
    public string? ChangesSummary { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response for AskQuestionWithCursorAsync
/// </summary>
public class AskQuestionWithCursorResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public string? Response { get; set; }
}

/// <summary>
/// Request for adding knowledge units to an existing plan
/// </summary>
public class AddKnowledgeUnitsToPlanRequest
{
    /// <summary>
    /// The unique identifier of the plan to add knowledge units to
    /// </summary>
    [Description("The unique identifier of the plan to add knowledge units to")]
    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Array of knowledge unit recommendations to add
    /// </summary>
    [Description("Array of knowledge unit recommendations to add")]
    [Required]
    [MinLength(1)]
    public required PlanKnowledgeUnitRecommendation[] Recommendations { get; set; }
}

/// <summary>
/// Response for AddKnowledgeUnitsToPlanAsync
/// </summary>
public class AddKnowledgeUnitsToPlanResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? Message { get; set; }
}
