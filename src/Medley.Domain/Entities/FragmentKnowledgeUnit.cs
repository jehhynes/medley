using System.ComponentModel.DataAnnotations.Schema;

namespace Medley.Domain.Entities;

/// <summary>
/// Join table entity representing the many-to-many relationship between Fragments and KnowledgeUnits
/// </summary>
public class FragmentKnowledgeUnit : BusinessEntity
{
    /// <summary>
    /// The ID of the fragment
    /// </summary>
    public Guid FragmentId { get; set; }

    /// <summary>
    /// Navigation to the fragment
    /// </summary>
    [ForeignKey(nameof(FragmentId))]
    public required virtual Fragment Fragment { get; set; }

    /// <summary>
    /// The ID of the knowledge unit
    /// </summary>
    public Guid KnowledgeUnitId { get; set; }

    /// <summary>
    /// Navigation to the knowledge unit
    /// </summary>
    [ForeignKey(nameof(KnowledgeUnitId))]
    public required virtual KnowledgeUnit KnowledgeUnit { get; set; }
}
