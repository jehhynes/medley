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

    /// <summary>
    /// Text content of the source (e.g., transcript data)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// JSON metadata from the external source (e.g., Fellow recording metadata)
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Date associated with the source (e.g., meeting date, creation date)
    /// </summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// The integration this source was imported from
    /// </summary>
    public virtual required Integration Integration { get; set; }

    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
}


