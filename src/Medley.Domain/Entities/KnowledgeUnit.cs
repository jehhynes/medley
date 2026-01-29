using Medley.Domain.Enums;
using Pgvector;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a canonical, validated aggregate of related Fragments representing synthesized knowledge from multiple sources
/// </summary>
public class KnowledgeUnit : BusinessEntity
{
    /// <summary>
    /// Title of the knowledge unit
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Brief summary of the knowledge unit
    /// </summary>
    [MaxLength(500)]
    public required string Summary { get; set; }

    /// <summary>
    /// The synthesized content of the knowledge unit
    /// </summary>
    [MaxLength(10000)]
    public required string Content { get; set; }

    /// <summary>
    /// Confidence level of the knowledge unit
    /// </summary>
    public ConfidenceLevel Confidence { get; set; }

    /// <summary>
    /// Explanation of factors affecting the confidence score
    /// </summary>
    [MaxLength(1000)]
    public string? ConfidenceComment { get; set; }

    /// <summary>
    /// Message or reasoning from the clustering process
    /// </summary>
    [MaxLength(2000)]
    public string? ClusteringComment { get; set; }

    /// <summary>
    /// The ID of the fragment category
    /// </summary>
    public Guid FragmentCategoryId { get; set; }

    /// <summary>
    /// Navigation to the fragment category
    /// </summary>
    [ForeignKey(nameof(FragmentCategoryId))]
    public required virtual FragmentCategory Category { get; set; }

    /// <summary>
    /// Vector embedding for semantic similarity operations
    /// </summary>
    [Column(TypeName = "vector(2000)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Date when the knowledge unit was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Fragments that belong to this knowledge unit
    /// </summary>
    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();

    /// <summary>
    /// Articles that reference this knowledge unit
    /// </summary>
    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}
