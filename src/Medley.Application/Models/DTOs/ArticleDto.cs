using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Summary information for Article entity (used in tree views and lists)
/// </summary>
public class ArticleSummaryDto
{
    /// <summary>
    /// Unique identifier for the article
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Article status
    /// </summary>
    public required ArticleStatus Status { get; set; }

    /// <summary>
    /// Parent article ID (null for root articles)
    /// </summary>
    public Guid? ParentArticleId { get; set; }

    /// <summary>
    /// Article type ID
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// User assigned to this article
    /// </summary>
    public UserSummaryDto? AssignedUser { get; set; }

    /// <summary>
    /// Child articles (for tree structure)
    /// </summary>
    public List<ArticleSummaryDto> Children { get; set; } = new();

    /// <summary>
    /// Current conversation summary
    /// </summary>
    public ConversationSummaryDto? CurrentConversation { get; set; }

    /// <summary>
    /// When the article was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the article was last modified
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Full article details including content
/// </summary>
public class ArticleDto
{
    /// <summary>
    /// Unique identifier for the article
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Article content (markdown)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Article status
    /// </summary>
    public required ArticleStatus Status { get; set; }

    /// <summary>
    /// Parent article ID (null for root articles)
    /// </summary>
    public Guid? ParentArticleId { get; set; }

    /// <summary>
    /// Current conversation summary
    /// </summary>
    public ConversationSummaryDto? CurrentConversation { get; set; }

    /// <summary>
    /// When the article was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Request to create a new article
/// </summary>
public class ArticleCreateRequest
{
    /// <summary>
    /// Article title
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; set; }

    /// <summary>
    /// Parent article ID (null for root articles)
    /// </summary>
    public Guid? ParentArticleId { get; set; }

    /// <summary>
    /// Article type ID
    /// </summary>
    public Guid? ArticleTypeId { get; set; }
}

/// <summary>
/// Request to update article metadata
/// </summary>
public class ArticleUpdateMetadataRequest
{
    /// <summary>
    /// Article title
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Title { get; set; }

    /// <summary>
    /// Article status
    /// </summary>
    public ArticleStatus? Status { get; set; }

    /// <summary>
    /// Article type ID
    /// </summary>
    public Guid? ArticleTypeId { get; set; }
}

/// <summary>
/// Request to update article content
/// </summary>
public class ArticleUpdateContentRequest
{
    /// <summary>
    /// Article content (markdown)
    /// </summary>
    [Required]
    public required string Content { get; set; }
}

/// <summary>
/// Request to move an article to a different parent
/// </summary>
public class ArticleMoveRequest
{
    /// <summary>
    /// New parent article ID
    /// </summary>
    public Guid? NewParentArticleId { get; set; }
}

/// <summary>
/// Article type information
/// </summary>
public class ArticleTypeDto
{
    /// <summary>
    /// Unique identifier for the article type
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article type name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Icon for the article type
    /// </summary>
    public string? Icon { get; set; }
}

/// <summary>
/// Response after moving an article
/// </summary>
public class ArticleMoveResponse
{
    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// ID of the article that was moved
    /// </summary>
    public required Guid ArticleId { get; set; }

    /// <summary>
    /// Previous parent article ID
    /// </summary>
    public Guid? OldParentId { get; set; }

    /// <summary>
    /// New parent article ID
    /// </summary>
    public required Guid NewParentId { get; set; }
}

/// <summary>
/// Response after deleting an article
/// </summary>
public class ArticleDeleteResponse
{
    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }
}

/// <summary>
/// Response after accepting or rejecting a version
/// </summary>
public class VersionActionResponse
{
    /// <summary>
    /// Indicates if the action was successful
    /// </summary>
    public required bool Success { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }
}