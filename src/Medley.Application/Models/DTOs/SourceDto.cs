using Medley.Domain.Enums;

namespace Medley.Application.Models.DTOs;

/// <summary>
/// Data transfer object for Source entity
/// </summary>
public class SourceDto
{
    /// <summary>
    /// Unique identifier for the source
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public SourceType Type { get; set; }

    /// <summary>
    /// Type of metadata stored in MetadataJson
    /// </summary>
    public SourceMetadataType MetadataType { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// External ID from the integration
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Text content of the source (e.g., transcript data)
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// JSON metadata from the external source
    /// </summary>
    public required string MetadataJson { get; set; }

    /// <summary>
    /// Date associated with the source (e.g., meeting date)
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Indicates whether this source is an internal meeting/document
    /// </summary>
    public bool? IsInternal { get; set; }

    /// <summary>
    /// Timestamp when smart tags were generated for this source
    /// </summary>
    public DateTimeOffset? TagsGenerated { get; set; }

    /// <summary>
    /// Status of fragment extraction for this source
    /// </summary>
    public ExtractionStatus ExtractionStatus { get; set; }

    /// <summary>
    /// Message from the fragment extraction process
    /// </summary>
    public string? ExtractionMessage { get; set; }

    /// <summary>
    /// Integration name this source was imported from
    /// </summary>
    public required string IntegrationName { get; set; }

    /// <summary>
    /// Number of fragments extracted from this source
    /// </summary>
    public int FragmentsCount { get; set; }

    /// <summary>
    /// When the source was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Tags associated with this source
    /// </summary>
    public List<SourceTagDto> Tags { get; set; } = new();
}

/// <summary>
/// Summary information about a source (for nested references)
/// </summary>
public class SourceSummaryDto
{
    /// <summary>
    /// Unique identifier for the source
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Source name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Source type
    /// </summary>
    public SourceType Type { get; set; }

    /// <summary>
    /// Date associated with the source
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Integration name
    /// </summary>
    public required string IntegrationName { get; set; }

    /// <summary>
    /// Number of fragments
    /// </summary>
    public int FragmentsCount { get; set; }

    /// <summary>
    /// Extraction status
    /// </summary>
    public ExtractionStatus ExtractionStatus { get; set; }
}

/// <summary>
/// Tag information for a source
/// </summary>
public class SourceTagDto
{
    /// <summary>
    /// Tag type ID
    /// </summary>
    public required Guid TagTypeId { get; set; }

    /// <summary>
    /// Tag type name
    /// </summary>
    public required string TagType { get; set; }

    /// <summary>
    /// Tag value
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Allowed value from tag options (if applicable)
    /// </summary>
    public string? AllowedValue { get; set; }
}
