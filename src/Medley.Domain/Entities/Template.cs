using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Template for AI prompts (fragment extraction, article generation, etc.)
/// </summary>
public class Template : BusinessEntity
{
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// The type of template (determines which AI workflow uses it)
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// The template content (markdown format)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Description of what this template does
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Date when the template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}


