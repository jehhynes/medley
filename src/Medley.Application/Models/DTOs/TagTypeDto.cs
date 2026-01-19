using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for TagType entity
/// </summary>
public class TagTypeDto
{
    /// <summary>
    /// Unique identifier for the tag type
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Tag type name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Prompt used for AI-based tag generation
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Indicates if this tag type is constrained to predefined values
    /// </summary>
    public required bool IsConstrained { get; set; }

    /// <summary>
    /// Mode for updating tag scope
    /// </summary>
    public required ScopeUpdateMode ScopeUpdateMode { get; set; }

    /// <summary>
    /// Allowed values for constrained tag types
    /// </summary>
    public required List<string> AllowedValues { get; set; } = new();
}
