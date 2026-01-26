using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Medley.Application.Models.DTOs.Llm.Tools;

/// <summary>
/// Request for creating an article improvement plan
/// </summary>
public class CreatePlanRequest
{
    /// <summary>
    /// Overall instructions for how to improve the article using the recommended fragments
    /// </summary>
    [Description("Overall instructions for how to improve the article using the recommended fragments in markdown format")]
    [Required]
    public required string Instructions { get; set; }

    /// <summary>
    /// Array of fragment recommendations
    /// </summary>
    [Description("Array of fragment recommendations")]
    [Required]
    [MinLength(1)]
    public required PlanFragmentRecommendation[] Recommendations { get; set; }

    /// <summary>
    /// Optional summary of changes if this is a modification of an existing plan
    /// </summary>
    [Description("Optional summary of changes if this is a modification of an existing plan")]
    [MaxLength(500)]
    public string? ChangesSummary { get; set; }
}

/// <summary>
/// Represents a fragment recommendation for a plan
/// </summary>
public class PlanFragmentRecommendation
{
    [Description("The unique identifier of the fragment being recommended")]
    public Guid FragmentId { get; set; }

    [Description("The similarity score between the fragment and the article (0.0 to 1.0)")]
    public double SimilarityScore { get; set; }

    [Description("Whether to include this fragment in the article improvement (true) or exclude it (false)")]
    public bool Include { get; set; }

    [Description("Explanation of why this fragment should not be included in the article. Omit if Include=true")]
    [MaxLength(200)]
    public string? Reasoning { get; set; }

    [Description("Optional instructions on how to incorporate this fragment into the article")]
    [MaxLength(200)]
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
    [MaxLength(200)]
    public required string ChangeMessage { get; set; }
}

/// <summary>
/// Fragment search result with similarity score (no content)
/// </summary>
public class ToolFragmentSearchResult : FragmentData
{
    public double SimilarityScore { get; set; }
}

/// <summary>
/// Response for SearchFragmentsAsync
/// </summary>
public class SearchFragmentsResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public List<ToolFragmentSearchResult> Fragments { get; set; } = new();
}

/// <summary>
/// Response for FindSimilarFragmentsAsync
/// </summary>
public class FindSimilarFragmentsResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public List<ToolFragmentSearchResult> Fragments { get; set; } = new();
}

/// <summary>
/// Response for GetFragmentContentAsync
/// </summary>
public class GetFragmentResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public FragmentWithContentData? Fragment { get; set; }
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
/// Request for adding fragments to an existing plan
/// </summary>
public class AddFragmentsToPlanRequest
{
    /// <summary>
    /// The unique identifier of the plan to add fragments to
    /// </summary>
    [Description("The unique identifier of the plan to add fragments to")]
    [Required]
    public Guid PlanId { get; set; }

    /// <summary>
    /// Array of fragment recommendations to add
    /// </summary>
    [Description("Array of fragment recommendations to add")]
    [Required]
    [MinLength(1)]
    public required PlanFragmentRecommendation[] Recommendations { get; set; }
}

/// <summary>
/// Response for AddFragmentsToPlanAsync
/// </summary>
public class AddFragmentsToPlanResponse
{
    public required bool Success { get; set; }
    public string? Error { get; set; }
    public int AddedCount { get; set; }
    public int SkippedCount { get; set; }
    public string? Message { get; set; }
}
