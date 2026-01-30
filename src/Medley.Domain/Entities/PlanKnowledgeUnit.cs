using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Join table linking Plans to KnowledgeUnits with AI recommendations
/// </summary>
[Index(nameof(PlanId), nameof(KnowledgeUnitId), IsUnique = true, Name = "IX_PlanKnowledgeUnits_PlanId_KnowledgeUnitId")]
public class PlanKnowledgeUnit : BaseEntity
{
    /// <summary>
    /// The plan this recommendation belongs to
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Navigation property to the plan
    /// </summary>
    [ForeignKey(nameof(PlanId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public required virtual Plan Plan { get; set; }

    /// <summary>
    /// The knowledge unit being recommended
    /// </summary>
    public Guid KnowledgeUnitId { get; set; }

    /// <summary>
    /// Navigation property to the knowledge unit
    /// </summary>
    [ForeignKey(nameof(KnowledgeUnitId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public required virtual KnowledgeUnit KnowledgeUnit { get; set; }

    /// <summary>
    /// Semantic similarity score between knowledge unit and article
    /// </summary>
    public required double SimilarityScore { get; set; }

    /// <summary>
    /// Whether to include this knowledge unit in the article (true) or just reference it (false)
    /// </summary>
    public required bool Include { get; set; }

    /// <summary>
    /// AI agent's reasoning for why this knowledge unit is recommended or not recommended
    /// </summary>
    [MaxLength(2000)]
    public string? Reasoning { get; set; }

    /// <summary>
    /// Instructions for how to use this knowledge unit
    /// </summary>
    [MaxLength(2000)]
    public string? Instructions { get; set; }
}
