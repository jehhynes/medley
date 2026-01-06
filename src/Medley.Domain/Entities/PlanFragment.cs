using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Join table linking Plans to Fragments with AI recommendations
/// </summary>
[Index(nameof(PlanId), nameof(FragmentId), IsUnique = true, Name = "IX_PlanFragments_PlanId_FragmentId")]
public class PlanFragment : BaseEntity
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
    public virtual Plan Plan { get; set; } = null!;

    /// <summary>
    /// The fragment being recommended
    /// </summary>
    public Guid FragmentId { get; set; }

    /// <summary>
    /// Navigation property to the fragment
    /// </summary>
    [ForeignKey(nameof(FragmentId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public virtual Fragment Fragment { get; set; } = null!;

    /// <summary>
    /// Semantic similarity score between fragment and article
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// Whether to include this fragment in the article (true) or just reference it (false)
    /// </summary>
    public bool Include { get; set; }

    /// <summary>
    /// AI agent's reasoning for why this fragment is recommended or not recommended
    /// </summary>
    [MaxLength(2000)]
    public required string Reasoning { get; set; }

    /// <summary>
    /// Instructions for how to use this fragment
    /// </summary>
    [MaxLength(2000)]
    public required string Instructions { get; set; }
}
