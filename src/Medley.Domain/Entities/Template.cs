using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Template for articles or insights generation
/// </summary>
public class Template : BusinessEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public required string Type { get; set; } // e.g., "Article", "Insight"

    public required string Content { get; set; } // template body
}


