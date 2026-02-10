using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a review of an article for approval workflow
/// </summary>
public class ArticleReview : BaseEntity
{
    /// <summary>
    /// The article being reviewed
    /// </summary>
    public Guid ArticleId { get; set; }
    
    /// <summary>
    /// Navigation property to the article being reviewed
    /// </summary>
    [ForeignKey(nameof(ArticleId))]
    [DeleteBehavior(DeleteBehavior.Cascade)]
    public Article Article { get; set; } = null!;
    
    /// <summary>
    /// User who performed the review
    /// </summary>
    public Guid ReviewedById { get; set; }
    
    /// <summary>
    /// Navigation property to the reviewing user
    /// </summary>
    [ForeignKey(nameof(ReviewedById))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public User ReviewedBy { get; set; } = null!;
    
    /// <summary>
    /// When the review was submitted
    /// </summary>
    public DateTimeOffset ReviewedAt { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// The action taken during review (Approve, RequestChanges, Comment)
    /// </summary>
    public ArticleReviewAction Action { get; set; }
    
    /// <summary>
    /// Optional comments provided with the review
    /// </summary>
    [MaxLength(2000)]
    public string? Comments { get; set; }
}
