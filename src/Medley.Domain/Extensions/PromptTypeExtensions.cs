using System.Reflection;
using Medley.Domain.Attributes;
using Medley.Domain.Enums;

namespace Medley.Domain.Extensions;

/// <summary>
/// Extension methods for PromptType enum
/// </summary>
public static class PromptTypeExtensions
{
    /// <summary>
    /// Gets the display name for a PromptType
    /// </summary>
    public static string GetName(this PromptType promptType)
    {
        var field = promptType.GetType().GetField(promptType.ToString());
        var attribute = field?.GetCustomAttribute<PromptTypeMetadataAttribute>();
        return attribute?.Name ?? promptType.ToString();
    }

    /// <summary>
    /// Gets the description for a PromptType
    /// </summary>
    public static string GetDescription(this PromptType promptType)
    {
        var field = promptType.GetType().GetField(promptType.ToString());
        var attribute = field?.GetCustomAttribute<PromptTypeMetadataAttribute>();
        return attribute?.Description ?? string.Empty;
    }

    /// <summary>
    /// Gets whether this prompt type is per-article-type
    /// </summary>
    public static bool GetIsPerArticleType(this PromptType promptType)
    {
        var field = promptType.GetType().GetField(promptType.ToString());
        var attribute = field?.GetCustomAttribute<PromptTypeMetadataAttribute>();
        return attribute?.IsPerArticleType ?? false;
    }

    /// <summary>
    /// Gets whether this prompt type is per-knowledge-category
    /// </summary>
    public static bool GetIsPerKnowledgeCategory(this PromptType promptType)
    {
        var field = promptType.GetType().GetField(promptType.ToString());
        var attribute = field?.GetCustomAttribute<PromptTypeMetadataAttribute>();
        return attribute?.IsPerKnowledgeCategory ?? false;
    }
}
