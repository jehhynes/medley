using Medley.Domain.Attributes;

namespace Medley.Domain.Enums;

/// <summary>
/// Types of AI prompt templates
/// </summary>
public enum PromptType
{
    /// <summary>
    /// Template for extracting fragments from source content
    /// </summary>
    [PromptTypeMetadata("Fragment Extraction", "Instructions for extracting knowledge fragments from sources")]
    FragmentExtraction = 1,

    /// <summary>
    /// Template for organization/tenant context used in prompts
    /// </summary>
    [PromptTypeMetadata("General Organization Context", "Helpful company or product information")]
    OrganizationContext = 2,

    /// <summary>
    /// Template for confidence scoring of extracted fragments
    /// </summary>
    [PromptTypeMetadata("Confidence Scoring", "Instructions for assigning a confidence score to fragments")]
    ConfidenceScoring = 3,

    /// <summary>
    /// Template for article improvement plan generation
    /// </summary>
    [PromptTypeMetadata("Plan Creation", "Plan Mode for creating article improvement plans")]
    ArticlePlanCreation = 4,

    /// <summary>
    /// Template for article chat assistant
    /// </summary>
    [PromptTypeMetadata("Agent Mode", "General Agent Mode instructions for articles")]
    ArticleAgentMode = 5,

    /// <summary>
    /// Template for implementing article improvement plans
    /// </summary>
    [PromptTypeMetadata("Plan Implementation", "Agent Mode when implementing article improvement plans")]
    ArticlePlanImplementation = 6,

    /// <summary>
    /// Template for article improvement plan generation specific to article types
    /// </summary>
    [PromptTypeMetadata("Article Types (Plan Mode)", "Plan mode instructions for specific article types", IsPerArticleType = true)]
    ArticleTypePlanMode = 7,

    /// <summary>
    /// Template for article chat assistant specific to article types
    /// </summary>
    [PromptTypeMetadata("Article Types (Agent Mode)", "Agent mode instructions for specific article types", IsPerArticleType = true)]
    ArticleTypeAgentMode = 8,

    /// <summary>
    /// Template for extracting fragments specific to fragment categories
    /// </summary>
    [PromptTypeMetadata("Fragment Categories", "Category-specific extraction instructions", IsPerFragmentCategory = true)]
    FragmentCategoryExtraction = 9
}

