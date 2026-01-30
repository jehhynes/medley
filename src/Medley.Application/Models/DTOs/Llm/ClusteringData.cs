using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Medley.Application.Services;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs.Llm;

/// <summary>
/// Overall input payload for fragment clustering containing all fragments to be clustered
/// </summary>
public class FragmentClusteringRequest
{
    [Required]
    public required string PrimaryGuidance { get; set; }

    [Required]
    public required string FragmentWeighting { get; set; }

    public required List<CategoryDefinition> CategoryDefinitions { get; set; }

    [Required]
    public required List<FragmentWithContentData> Fragments { get; set; }
}

/// <summary>
/// Response from fragment clustering LLM call containing multiple knowledge units
/// </summary>
public class FragmentClusteringResponse
{
    [Required]
    [Description("List of knowledge units created from the provided fragments. Can be empty if no valid clusters found.")]
    public required List<KnowledgeUnitCluster> KnowledgeUnits { get; set; }

    [MaxLength(2000)]
    [Description("Overall reasoning about the clustering decisions, including any fragments that were excluded")]
    public string? Message { get; set; }
}

/// <summary>
/// Represents a single knowledge unit cluster
/// </summary>
public class KnowledgeUnitCluster
{
    [Required]
    [MinLength(2)]
    [Description("List of fragment IDs that belong to this knowledge unit. Must contain at least 2 fragments.")]
    public required List<Guid> FragmentIds { get; set; }

    [Required]
    [MaxLength(200)]
    [Description("Clear, descriptive heading for the clustered content")]
    public required string Title { get; set; }

    [Required]
    [MaxLength(500)]
    [Description("Short, human-readable condensation of the full content")]
    public required string Summary { get; set; }

    [Required]
    [Description("The knowledge category which is most appropriate for the consolidated content")]
    public required string Category { get; set; }

    [Required]
    [Description("The full consolidated text content synthesizing all fragments")]
    public required string Content { get; set; }

    [Required]
    [Description("Confidence level for this cluster")]
    public required ConfidenceLevel Confidence { get; set; }

    [MaxLength(1000)]
    [Description("Explanation of the confidence level and any concerns or caveats about the clustered content")]
    public string? ConfidenceComment { get; set; }

    [MaxLength(2000)]
    [Description("Reasoning for why these specific fragments were grouped together")]
    public string? ClusteringRationale { get; set; }
}
