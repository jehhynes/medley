using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Allowed value for a TagType (used as either a constraint or a suggestion).
/// </summary>
public class TagOption : BusinessEntity
{
    public Guid TagTypeId { get; set; }
    public virtual required TagType TagType { get; set; }

    [MaxLength(200)]
    public required string Value { get; set; }
}

