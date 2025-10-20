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
    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
}