using Medley.Domain.Attributes;

namespace Medley.Domain.Enums;

/// <summary>
/// Types of AI prompt templates
/// </summary>
public enum TemplateType
{
    /// <summary>
    /// Template for extracting fragments from source content
    /// </summary>
    [TemplateTypeMetadata("Fragment Extraction Prompt", "Instructions for extracting knowledge fragments from source content")]
    FragmentExtraction = 1,

    /// <summary>
    /// Template for organization/tenant context used in prompts
    /// </summary>
    [TemplateTypeMetadata("Organization Context", "Helpful information about your company or product")]
    OrganizationContext = 2,

    /// <summary>
    /// Template for confidence scoring of extracted fragments
    /// </summary>
    [TemplateTypeMetadata("Confidence Scoring Prompt", "Instructions for assigning a confidence score to extracted fragments")]
    ConfidenceScoring = 3,

    /// <summary>
    /// Template for article improvement plan generation
    /// </summary>
    [TemplateTypeMetadata("Article Plan Creation", "Template for creating article improvement plans")]
    ArticlePlanCreation = 4,

    /// <summary>
    /// Template for article chat assistant
    /// </summary>
    [TemplateTypeMetadata("Article Chat", "Template for general chat mode on an article")]
    ArticleChat = 5,

    /// <summary>
    /// Template for implementing article improvement plans
    /// </summary>
    [TemplateTypeMetadata("Article Plan Implementation", "Template for implementing article improvement plans with AI assistance")]
    ArticlePlanImplementation = 6,

    /// <summary>
    /// Template for article improvement plan generation specific to article types
    /// </summary>
    [TemplateTypeMetadata("Article Type (Plan Mode)", "Template for creating article improvement plans specific to article types", IsPerArticleType = true)]
    ArticleTypePlanMode = 7,

    /// <summary>
    /// Template for article chat assistant specific to article types
    /// </summary>
    [TemplateTypeMetadata("Article Type (Agent Mode)", "Template for chat/agent mode specific to article types", IsPerArticleType = true)]
    ArticleTypeAgentMode = 8
}

