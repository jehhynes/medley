using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a category of fragment (e.g., Tutorial, How-To, Concept, Best Practice)
/// </summary>
[Index(nameof(Name), IsUnique = true)]
public class FragmentCategory : BusinessEntity
{
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }
}
