using Medley.Domain.Enums;
using Medley.Domain.Extensions;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Summary information about a template (for list views)
/// </summary>
public class TemplateListDto
{
    /// <summary>
    /// Unique identifier for the template
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Display name of the template
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Template type enum value
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// Template type name as string
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this template was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

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
    /// Unique identifier for the template
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Display name of the template
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Template type enum value
    /// </summary>
    public required TemplateType Type { get; set; }

    /// <summary>
    /// Template type name as string
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Description of the template
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Template content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// When this template was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }
}
