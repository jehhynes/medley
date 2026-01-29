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
    public required Guid PrimaryFragmentId { get; set; }

    [Required]
    public required List<FragmentWithContentData> Fragments { get; set; }
}

/// <summary>
/// Response from fragment clustering LLM call
/// </summary>
public class FragmentClusteringResponse
{
    [Required]
    [Description("List of fragment IDs that should be included in this cluster. Exclude any unrelated fragments.")]
    public required List<Guid> IncludedFragmentIds { get; set; }

    [Required]
    [MaxLength(200)]
    [Description("Clear, descriptive heading for the clustered content")]
    public required string Title { get; set; }

    [Required]
    [MaxLength(500)]
    [Description("Short, human-readable condensation of the full content")]
    public required string Summary { get; set; }

    [Required]
    [Description("The fragment category which is most appropriate for the consolidated content")]
    public required string Category { get; set; }

    [Required]
    [Description("The full consolidated text content")]
    public required string Content { get; set; }

    [Required]
    [Description("Confidence level for this cluster")]
    public required ConfidenceLevel Confidence { get; set; }

    [MaxLength(1000)]
    [Description("Explanation of the confidence level and any concerns or caveats about the clustered content")]
    public string? ConfidenceComment { get; set; }
    
    [MaxLength(2000)]
    [Description("Any auxiliary information, reasoning, or comments")]
    public string? Message { get; set; }
}
