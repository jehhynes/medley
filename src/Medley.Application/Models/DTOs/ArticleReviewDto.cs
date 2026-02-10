using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Article review information
/// </summary>
public class ArticleReviewDto
{
    /// <summary>
    /// Unique identifier for the review
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Article ID that was reviewed
    /// </summary>
    public required Guid ArticleId { get; set; }

    /// <summary>
    /// User who performed the review
    /// </summary>
    public required UserSummaryDto ReviewedBy { get; set; }

    /// <summary>
    /// When the review was submitted
    /// </summary>
    public required DateTimeOffset ReviewedAt { get; set; }

    /// <summary>
    /// Review action (Comment, Approve, RequestChanges)
    /// </summary>
    public required ArticleReviewAction Action { get; set; }

    /// <summary>
    /// Optional review comments (HTML format from Tiptap editor)
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// Request to create a new article review
/// </summary>
public class CreateArticleReviewRequest
{
    /// <summary>
    /// Review action (Comment, Approve, RequestChanges)
    /// </summary>
    [Required]
    public required ArticleReviewAction Action { get; set; }

    /// <summary>
    /// Optional review comments (HTML format from Tiptap editor)
    /// </summary>
    [StringLength(2000)]
    public string? Comments { get; set; }

    /// <summary>
    /// Optional user ID to reassign the article to (overrides automatic successor assignment)
    /// </summary>
    public Guid? ReassignToUserId { get; set; }
}

/// <summary>
/// Response after creating a review
/// </summary>
public class CreateArticleReviewResponse
{
    /// <summary>
    /// The created review
    /// </summary>
    public required ArticleReviewDto Review { get; set; }

    /// <summary>
    /// Indicates if the article was auto-approved
    /// </summary>
    public required bool ArticleApproved { get; set; }

    /// <summary>
    /// Indicates if the article was reassigned to a successor
    /// </summary>
    public required bool ArticleReassigned { get; set; }

    /// <summary>
    /// User ID the article was reassigned to (if reassigned)
    /// </summary>
    public Guid? ReassignedToUserId { get; set; }

    /// <summary>
    /// Status message
    /// </summary>
    public required string Message { get; set; }
}
