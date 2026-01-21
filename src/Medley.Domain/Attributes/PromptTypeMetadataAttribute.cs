namespace Medley.Domain.Attributes;

/// <summary>
/// Attribute to define metadata for PromptType enum values
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class PromptTypeMetadataAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public bool IsPerArticleType { get; set; }

    public PromptTypeMetadataAttribute(string name, string description)
    {
        Name = name;
        Description = description;
        IsPerArticleType = false;
    }
}
