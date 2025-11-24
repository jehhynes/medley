using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// Composed document built from Fragments
/// </summary>
public class Article : BusinessEntity
{
    [MaxLength(200)]
    public required string Title { get; set; }

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
    /// Navigation property to child articles
    /// </summary>
    public virtual ICollection<Article> ChildArticles { get; set; } = new List<Article>();
    
    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
}