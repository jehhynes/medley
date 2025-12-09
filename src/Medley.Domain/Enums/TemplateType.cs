namespace Medley.Domain.Enums;

/// <summary>
/// Types of AI prompt templates
/// </summary>
public enum TemplateType
{
    /// <summary>
    /// Template for extracting fragments from source content
    /// </summary>
    FragmentExtraction = 1,

    /// Template for organization/tenant context used in prompts
    /// </summary>
    OrganizationContext = 2
}

