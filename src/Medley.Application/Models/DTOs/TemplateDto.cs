using Medley.Domain.Enums;
using Medley.Domain.Extensions;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Summary information about a template (for list views)
/// </summary>
public class TemplateListDto
{
    /// <summary>
    /// Unique identifier for the template (null if doesn't exist yet)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Display name of the template
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Template type enum value
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this template type is per-article-type
    /// </summary>
    public required bool IsPerArticleType { get; set; }

    /// <summary>
    /// Article type ID (for per-article-type templates)
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// Article type name (for per-article-type templates)
    /// </summary>
    public string? ArticleTypeName { get; set; }

    /// <summary>
    /// Whether this template exists in the database
    /// </summary>
    public required bool Exists { get; set; }

    /// <summary>
    /// When this template was created
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// When this template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}

/// <summary>
/// Full template information including content
/// </summary>
public class TemplateDto
{
    /// <summary>
    /// Unique identifier for the template (null if doesn't exist yet)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Display name of the template
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Template type enum value
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this template type is per-article-type
    /// </summary>
    public required bool IsPerArticleType { get; set; }

    /// <summary>
    /// Article type ID (for per-article-type templates)
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// Article type name (for per-article-type templates)
    /// </summary>
    public string? ArticleTypeName { get; set; }

    /// <summary>
    /// Template content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Whether this template exists in the database
    /// </summary>
    public required bool Exists { get; set; }

    /// <summary>
    /// When this template was created
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// When this template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}
