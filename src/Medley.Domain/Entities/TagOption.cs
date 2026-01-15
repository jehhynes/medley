using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Allowed value for a TagType (used as either a constraint or a suggestion).
/// </summary>
[Index(nameof(TagTypeId), nameof(Value), IsUnique = true)]
public class TagOption : BusinessEntity
{
    protected Guid TagTypeId { get; set; }
    
    [ForeignKey(nameof(TagTypeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual required TagType TagType { get; set; }

    [MaxLength(200)]
    public required string Value { get; set; }
}

