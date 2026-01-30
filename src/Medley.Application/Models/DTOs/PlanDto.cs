using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Minimal plan reference with just ID and status
/// </summary>
public class PlanRef
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Current status of the plan
    /// </summary>
    public required PlanStatus Status { get; set; }
}

/// <summary>
/// Detailed plan information with knowledge units
/// </summary>
public class PlanDto
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article ID this plan is for
    /// </summary>
    public required Guid ArticleId { get; set; }

    /// <summary>
    /// Markdown-formatted instructions from the AI agent
    /// </summary>
    public required string Instructions { get; set; }

    /// <summary>
    /// Current status of the plan
    /// </summary>
    public required PlanStatus Status { get; set; }

    /// <summary>
    /// Version number of this plan (per article)
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// AI-generated summary of changes from parent plan
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// When this plan was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// User who created this plan
    /// </summary>
    public required UserRef CreatedBy { get; set; }

    /// <summary>
    /// Knowledge unit recommendations for this plan
    /// </summary>
    public required List<PlanKnowledgeUnitDto> KnowledgeUnits { get; set; } = new();
}

/// <summary>
/// Detailed knowledge unit information in a plan
/// </summary>
public class PlanKnowledgeUnitDto
{
    /// <summary>
    /// Plan knowledge unit ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Knowledge unit ID
    /// </summary>
    public required Guid KnowledgeUnitId { get; set; }

    /// <summary>
    /// Similarity score (0-1)
    /// </summary>
    public required double SimilarityScore { get; set; }

    /// <summary>
    /// Whether to include this knowledge unit in the implementation
    /// </summary>
    public required bool Include { get; set; }

    /// <summary>
    /// AI reasoning for including this knowledge unit
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Specific instructions for using this knowledge unit
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Knowledge unit details
    /// </summary>
    public required KnowledgeUnitInPlanDto KnowledgeUnit { get; set; }
}

/// <summary>
/// Knowledge unit information within a plan
/// </summary>
public class KnowledgeUnitInPlanDto
{
    /// <summary>
    /// Knowledge unit ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Knowledge unit title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Knowledge unit summary
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Knowledge unit category
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Icon for the knowledge unit category
    /// </summary>
    public string? CategoryIcon { get; set; }

    /// <summary>
    /// Knowledge unit content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public ConfidenceLevel? Confidence { get; set; }

    /// <summary>
    /// Confidence comment
    /// </summary>
    public string? ConfidenceComment { get; set; }
}

/// <summary>
/// Summary information about a plan (for list views)
/// </summary>
public class PlanSummaryDto
{
    /// <summary>
    /// Unique identifier for the plan
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Version number of this plan (per article)
    /// </summary>
    public required int Version { get; set; }

    /// <summary>
    /// Current status of the plan
    /// </summary>
    public required PlanStatus Status { get; set; }

    /// <summary>
    /// When this plan was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// AI-generated summary of changes from parent plan
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// User who created this plan
    /// </summary>
    public required UserRef CreatedBy { get; set; }
}

/// <summary>
/// Response after performing an action on a plan
/// </summary>
public class PlanActionResponse
{
    /// <summary>
    /// Indicates if the action was successful
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// ID of the conversation created for implementation (when accepting a plan)
    /// </summary>
    public Guid ConversationId { get; set; }
}