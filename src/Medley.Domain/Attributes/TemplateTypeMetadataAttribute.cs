namespace Medley.Domain.Attributes;

/// <summary>
/// Attribute to define metadata for TemplateType enum values
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class TemplateTypeMetadataAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }

    public TemplateTypeMetadataAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
