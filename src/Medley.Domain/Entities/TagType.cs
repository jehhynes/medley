using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Defines a structured tag category with optional AI prompt guidance and allowed values.
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class TagType : BusinessEntity
{
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Instructions for the AI on how to determine this tag's value.
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// When true, AI/user must pick from allowed values; when false, values are suggestions only.
    /// </summary>
    public required bool IsConstrained { get; set; }

    /// <summary>
    /// Determines how this tag type affects the source's IsInternal property when scope is unknown (IsInternal == null).
    /// </summary>
    public required ScopeUpdateMode ScopeUpdateMode { get; set; } = ScopeUpdateMode.None;

    public virtual ICollection<TagOption> AllowedValues { get; set; } = new List<TagOption>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

