using System.ComponentModel.DataAnnotations;

namespace Medley.Domain.Entities;

/// <summary>
/// Represents a type of article (e.g., Index, HowTo, Tutorial, Reference, Concept, FAQ)
/// </summary>
public class ArticleType : BusinessEntity
{
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(50)]
    public string? Icon { get; set; }

    public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
}

