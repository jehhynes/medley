using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Template for AI prompts (fragment extraction, article generation, etc.)
/// </summary>
public class Template : BusinessEntity
{
    /// <summary>
    /// The type of template (determines which AI workflow uses it)
    /// Name and description are defined on the TemplateType enum
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// The template content (markdown format)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional article type ID for per-article-type templates
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// Navigation property to the article type
    /// </summary>
    public ArticleType? ArticleType { get; set; }

    /// <summary>
    /// Date when the template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}


