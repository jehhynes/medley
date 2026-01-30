using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Template for AI prompts (fragment extraction, article generation, etc.)
/// </summary>
public class AiPrompt : BusinessEntity
{
    /// <summary>
    /// The type of template (determines which AI workflow uses it)
    /// Name and description are defined on the PromptType enum
    /// </summary>
    public required PromptType Type { get; set; }

    /// <summary>
    /// The template content (markdown format)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional article type ID for per-article-type templates
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// Navigation property to the article type
    /// </summary>
    public virtual ArticleType? ArticleType { get; set; }

    /// <summary>
    /// Optional knowledge category ID for per-knowledge-category templates
    /// </summary>
    public Guid? KnowledgeCategoryId { get; set; }

    /// <summary>
    /// Navigation property to the knowledge category
    /// </summary>
    public virtual KnowledgeCategory? KnowledgeCategory { get; set; }

    /// <summary>
    /// Date when the template was last modified
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// Date when the prompt was last synced to Cursor workspace
    /// </summary>
    public DateTimeOffset? LastSyncedWithCursor { get; set; }
}


