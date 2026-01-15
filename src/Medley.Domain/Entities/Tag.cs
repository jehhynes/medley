using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Tag assigned to a source; one tag per tag type per source.
/// </summary>
[Index(nameof(SourceId), nameof(TagTypeId), IsUnique = true)]
public class Tag : BusinessEntity
{
    protected Guid SourceId { get; set; }
    
    [ForeignKey(nameof(SourceId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual required Source Source { get; set; }

    protected Guid TagTypeId { get; set; }
    
    [ForeignKey(nameof(TagTypeId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public virtual required TagType TagType { get; set; }

    protected Guid? TagOptionId { get; set; }
    
    [ForeignKey(nameof(TagOptionId))]
    [DeleteBehavior(DeleteBehavior.SetNull)]
    public virtual TagOption? TagOption { get; set; }

    [MaxLength(200)]
    public required string Value { get; set; }
}

