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

    /// <summary>
    /// Template for organization/tenant context used in prompts
    /// </summary>
    OrganizationContext = 2,

    /// <summary>
    /// Template for confidence scoring of extracted fragments
    /// </summary>
    ConfidenceScoring = 3,

    /// <summary>
    /// Template for article improvement plan generation
    /// </summary>
    ArticleImprovementPlan = 4
}

