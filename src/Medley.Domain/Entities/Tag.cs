using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Tag assigned to a source; one tag per tag type per source.
/// </summary>
public class Tag : BusinessEntity
{
    public Guid SourceId { get; set; }
    public virtual required Source Source { get; set; }

    public Guid TagTypeId { get; set; }
    public virtual required TagType TagType { get; set; }

    public Guid? TagOptionId { get; set; }
    public virtual TagOption? TagOption { get; set; }

    [MaxLength(200)]
    public required string Value { get; set; }
}

