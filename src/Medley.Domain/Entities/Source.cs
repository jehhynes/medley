using System.ComponentModel.DataAnnotations;
using Medley.Domain.Enums;

namespace Medley.Domain.Entities;

/// <summary>
/// A concrete data source within an integration (e.g., a repo, calendar, meeting)
/// </summary>
public class Source : BusinessEntity
{
    public required SourceType Type { get; set; }

    /// <summary>
    /// Type of metadata stored in MetadataJson (allows consumers to know which model to deserialize into)
    /// </summary>
    public required SourceMetadataType MetadataType { get; set; }

    [MaxLength(200)]
    public required string Name { get; set; }

    [MaxLength(400)]
    public string? ExternalId { get; set; }

    /// <summary>
    /// Text content of the source (e.g., transcript data)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// JSON metadata from the external source (e.g., Fellow recording metadata)
    /// </summary>
    public required string MetadataJson { get; set; }

    /// <summary>
    /// Date associated with the source (e.g., meeting date, creation date)
    /// </summary>
    public required DateTimeOffset Date { get; set; }

    /// <summary>
    /// Indicates whether this source is an internal meeting/document (all attendees from same org)
    /// null = not yet determined, true = internal, false = external
    /// </summary>
    public bool? IsInternal { get; set; }

    /// <summary>
    /// Timestamp when smart tags were generated for this source.
    /// </summary>
    public DateTimeOffset? TagsGenerated { get; set; }

    /// <summary>
    /// Timestamp when speakers were extracted from this source.
    /// </summary>
    public DateTimeOffset? SpeakersExtracted { get; set; }

    /// <summary>
    /// The primary speaker for this source (determined by total transcript length)
    /// </summary>
    public Guid? PrimarySpeakerId { get; set; }
    public virtual Speaker? PrimarySpeaker { get; set; }

    /// <summary>
    /// Status of fragment extraction for this source
    /// </summary>
    public ExtractionStatus ExtractionStatus { get; set; } = ExtractionStatus.NotStarted;

    /// <summary>
    /// Message from the fragment extraction process (e.g., explanation when no fragments found)
    /// </summary>
    public string? ExtractionMessage { get; set; }

    /// <summary>
    /// Indicates if this source has been deleted (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// The integration this source was imported from
    /// </summary>
    public virtual required Integration Integration { get; set; }

    public virtual ICollection<Fragment> Fragments { get; set; } = new List<Fragment>();
    public virtual ICollection<Observation> Observations { get; set; } = new List<Observation>();
    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public virtual ICollection<Speaker> Speakers { get; set; } = new List<Speaker>();
}


