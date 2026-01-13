using System.Reflection;
using Medley.Domain.Attributes;
using Medley.Domain.Enums;

namespace Medley.Domain.Extensions;

/// <summary>
/// Extension methods for TemplateType enum
/// </summary>
public static class TemplateTypeExtensions
{
    /// <summary>
    /// Gets the display name for a TemplateType
    /// </summary>
    public static string GetName(this TemplateType templateType)
    {
        var field = templateType.GetType().GetField(templateType.ToString());
        var attribute = field?.GetCustomAttribute<TemplateTypeMetadataAttribute>();
        return attribute?.Name ?? templateType.ToString();
    }

    /// <summary>
    /// Gets the description for a TemplateType
    /// </summary>
    public static string GetDescription(this TemplateType templateType)
    {
        var field = templateType.GetType().GetField(templateType.ToString());
        var attribute = field?.GetCustomAttribute<TemplateTypeMetadataAttribute>();
        return attribute?.Description ?? string.Empty;
    }
}
