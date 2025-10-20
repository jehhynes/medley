using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// A concrete data source within an integration (e.g., a repo, calendar, meeting)
/// </summary>
public class Source : BusinessEntity
{
    public required SourceType Type { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(400)]
    public string? ExternalId { get; set; }

    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
}


