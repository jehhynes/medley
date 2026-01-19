using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for Plan entity
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
    /// Conversation ID that created this plan (optional)
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Markdown-formatted instructions from the AI agent
    /// </summary>
    public required string Instructions { get; set; }

    /// <summary>
    /// Current status of the plan
    /// </summary>
    public PlanStatus Status { get; set; }

    /// <summary>
    /// When this plan was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// User who created this plan
    /// </summary>
    public required UserSummaryDto CreatedBy { get; set; }

    /// <summary>
    /// When this plan was applied to the article (if applicable)
    /// </summary>
    public DateTimeOffset? AppliedAt { get; set; }

    /// <summary>
    /// Version number of this plan (per article)
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Parent plan ID this was derived from (if modified)
    /// </summary>
    public Guid? ParentPlanId { get; set; }

    /// <summary>
    /// AI-generated summary of changes from parent plan
    /// </summary>
    public string? ChangesSummary { get; set; }

    /// <summary>
    /// Fragment recommendations for this plan
    /// </summary>
    public List<PlanFragmentDto> Fragments { get; set; } = new();
}

/// <summary>
/// Fragment recommendation in a plan
/// </summary>
public class PlanFragmentDto
{
    /// <summary>
    /// Fragment ID
    /// </summary>
    public required Guid FragmentId { get; set; }

    /// <summary>
    /// Fragment title
    /// </summary>
    public required string FragmentTitle { get; set; }

    /// <summary>
    /// Fragment summary
    /// </summary>
    public required string FragmentSummary { get; set; }

    /// <summary>
    /// Relevance score (0-1)
    /// </summary>
    public double RelevanceScore { get; set; }
}

/// <summary>
/// Detailed plan information with fragments and source details
/// </summary>
public class PlanDetailDto
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
    public required string Status { get; set; }

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
    public required UserSummaryDto CreatedBy { get; set; }

    /// <summary>
    /// Fragment recommendations for this plan
    /// </summary>
    public required List<PlanFragmentDetailDto> Fragments { get; set; } = new();
}

/// <summary>
/// Detailed fragment information in a plan
/// </summary>
public class PlanFragmentDetailDto
{
    /// <summary>
    /// Plan fragment ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Fragment ID
    /// </summary>
    public required Guid FragmentId { get; set; }

    /// <summary>
    /// Similarity score (0-1)
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Whether to include this fragment in the implementation
    /// </summary>
    public bool Include { get; set; }

    /// <summary>
    /// AI reasoning for including this fragment
    /// </summary>
    public string? Reasoning { get; set; }

    /// <summary>
    /// Specific instructions for using this fragment
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// Fragment details
    /// </summary>
    public required FragmentInPlanDto Fragment { get; set; }
}

/// <summary>
/// Fragment information within a plan
/// </summary>
public class FragmentInPlanDto
{
    /// <summary>
    /// Fragment ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Fragment title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Fragment summary
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Fragment category
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Fragment content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Confidence level
    /// </summary>
    public string? Confidence { get; set; }

    /// <summary>
    /// Confidence comment
    /// </summary>
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Source information
    /// </summary>
    public SourceInPlanDto? Source { get; set; }
}

/// <summary>
/// Source information within a plan fragment
/// </summary>
public class SourceInPlanDto
{
    /// <summary>
    /// Source ID
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Source date
    /// </summary>
    public DateTimeOffset Date { get; set; }
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
    public required string Status { get; set; }

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
    public required UserSummaryDto CreatedBy { get; set; }
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
    public Guid? ConversationId { get; set; }
}