using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Composed document built from Fragments
/// </summary>
public class Article : BusinessEntity
{
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(1000)]
    public string? Summary { get; set; }

    public string? Content { get; set; }

    /// <summary>
    /// JSON-serialized metadata from Knowledge Builder import
    /// </summary>
    public string? Metadata { get; set; }

    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public DateTimeOffset? PublishedAt { get; set; }
    
    /// <summary>
    /// Parent article ID for nested/hierarchical articles (null for root-level articles)
    /// </summary>
    public Guid? ParentArticleId { get; set; }

    /// <summary>
    /// Navigation property to parent article
    /// </summary>
    public virtual Article? ParentArticle { get; set; }

    /// <summary>
    /// Foreign key to ArticleType
    /// </summary>
    public Guid? ArticleTypeId { get; set; }

    /// <summary>
    /// Navigation property to ArticleType
    /// </summary>
    [ForeignKey(nameof(ArticleTypeId))]
    public virtual ArticleType? ArticleType { get; set; }

    
    /// <summary>
    /// Navigation property to child articles
    /// </summary>
    public virtual ICollection<Article> ChildArticles { get; set; } = new List<Article>();
        
    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
    
    /// <summary>
    /// Chat conversations about this article
    /// </summary>
    public virtual ICollection<ChatConversation> ChatConversations { get; set; } = new List<ChatConversation>();
}