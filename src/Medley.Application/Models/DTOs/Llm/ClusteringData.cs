using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Medley.Application.Services;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs.Llm;

/// <summary>
/// Fragment data for clustering with integer ID instead of GUID
/// </summary>
public class ClusteringFragmentData
{
    public required int Id { get; set; }
    public required string Title { get; set; }
    public string? Category { get; set; }
    public required string Content { get; set; }
    public ConfidenceLevel? Confidence { get; set; }
    public string? ConfidenceComment { get; set; }
    public SourceData? Source { get; set; }
}

/// <summary>
/// System prompt guidance for fragment clustering containing instructions and category definitions
/// </summary>
public class FragmentClusteringGuidance
{
    [Required]
    public required string PrimaryGuidance { get; set; }

    [Required]
    public required string FragmentWeighting { get; set; }

    public string? OrganizationContext { get; set; }

    public required List<CategoryDefinition> CategoryDefinitions { get; set; }
}

/// <summary>
/// User prompt for fragment clustering containing the fragments to be clustered
/// </summary>
public class FragmentClusteringRequest
{
    [Required]
    public required List<ClusteringFragmentData> Fragments { get; set; }
}

/// <summary>
/// Response from fragment clustering LLM call containing multiple knowledge units
/// </summary>
public class FragmentClusteringResponse
{
    [Required]
    [Description("List of knowledge units created from the provided fragments. Can be empty if no valid clusters found.")]
    public required List<KnowledgeUnitCluster> KnowledgeUnits { get; set; }

    [Description("List of fragment IDs that were excluded from clustering with explanations")]
    public List<ExcludedFragment>? ExcludedFragments { get; set; }

    [MaxLength(200)]
    [Description("Overall reasoning about the clustering decisions")]
    public string? Message { get; set; }
}

/// <summary>
/// Represents a fragment that was excluded from clustering
/// </summary>
public class ExcludedFragment
{
    [Required]
    [Description("The fragment ID (integer from the provided fragments) that was excluded")]
    public required int FragmentId { get; set; }

    [Required]
    [MaxLength(500)]
    [Description("Clear explanation of why this fragment was excluded from clustering")]
    public required string Reason { get; set; }
}

/// <summary>
/// Represents a single knowledge unit cluster
/// </summary>
public class KnowledgeUnitCluster
{
    [Required]
    [MinLength(2)]
    [Description("List of fragment IDs (integers from the provided fragments) that belong to this knowledge unit. Must contain at least 2 fragments.")]
    public required List<int> FragmentIds { get; set; }

    [Required]
    [MaxLength(75)]
    [Description("Clear, descriptive heading for the clustered content")]
    public required string Title { get; set; }

    [Required]
    [MaxLength(250)]
    [Description("Short, human-readable condensation of the full content")]
    public required string Summary { get; set; }

    [Required]
    [Description("The knowledge category which is most appropriate for the consolidated content")]
    public required string Category { get; set; }

    [MaxLength(2000)]
    [Required]
    [Description("The consolidated text content synthesizing all fragments")]
    public required string Content { get; set; }

    [Required]
    [Description("Confidence level for this cluster")]
    public required ConfidenceLevel Confidence { get; set; }

    [MaxLength(200)]
    [Description("Explanation of the confidence level and any concerns or caveats about the clustered content")]
    public string? ConfidenceComment { get; set; }

    [MaxLength(200)]
    [Description("Reasoning for why these specific fragments were grouped together")]
    public string? ClusteringRationale { get; set; }
}
